#Requires -Version 5.1
<#
.SYNOPSIS
  Copies everything you can from GitHub onto this machine: git refs, Release ZIPs, optional CI artifact.

.DESCRIPTION
  A normal git clone already has source, history, branches, and tags. GitHub also stores
  Release assets and Actions artifacts *outside* git (they are gitignored here on purpose).

  This script:
    1. Fetches all remote branches and tags into the current clone (or clones a fresh copy).
    2. Downloads every GitHub Release asset into github-mirror/releases/<tag>/ (gitignored).
    3. Optionally downloads the newest successful main CI portable ZIP.

  Issues, pull requests, Actions logs, and GitHub secrets are not git objects and are not copied.

.PARAMETER GithubRepo
  owner/name (default: devildog5x5/ONVIF-ODM).

.PARAMETER OutDir
  Folder for downloaded ZIPs, relative to the repo root unless absolute.
  Default: github-mirror (gitignored).

.PARAMETER CloneTo
  If set, git clone this repo into that folder first (working tree), then fetch and download there.

.PARAMETER BareMirror
  With -CloneTo, use `git clone --mirror` (bare backup, no working files). Release downloads still
  go to -OutDir.

.PARAMETER SkipReleases
  Fetch git only; do not download Release ZIPs.

.PARAMETER IncludeCiArtifact
  Also download the newest successful main Build workflow artifact (Windows portable ZIP).

.PARAMETER Tags
  Optional tag names to download (e.g. v2.0.0). Default: every release.

.PARAMETER DryRun
  Print what would be fetched/downloaded; write nothing.

.EXAMPLE
  .\scripts\mirror-github-locally.ps1

.EXAMPLE
  .\scripts\mirror-github-locally.ps1 -IncludeCiArtifact

.EXAMPLE
  .\scripts\mirror-github-locally.ps1 -CloneTo D:\mirrors\ONVIF-ODM -BareMirror
#>
[CmdletBinding()]
param(
    [string] $GithubRepo = "devildog5x5/ONVIF-ODM",
    [string] $OutDir = "github-mirror",
    [string] $CloneTo = "",
    [switch] $BareMirror,
    [switch] $SkipReleases,
    [switch] $IncludeCiArtifact,
    [string[]] $Tags = @(),
    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-CommandOnPath {
    param([string] $Name)
    $null = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $?) {
        throw "'$Name' not found on PATH."
    }
}

function Get-RepoRoot {
    $root = (git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $root) {
        throw "Run this from a git working tree, or pass -CloneTo to create one."
    }
    return $root.Trim()
}

function Invoke-GitFetchAll {
    param([string] $WorkDir)
    Write-Host "Fetching all remotes, branches, and tags in $WorkDir ..." -ForegroundColor Yellow
    if ($DryRun) { return }
    Push-Location $WorkDir
    try {
        git fetch --all --tags --prune
        if ($LASTEXITCODE -ne 0) { throw "git fetch --all --tags --prune failed." }
    }
    finally {
        Pop-Location
    }
}

Test-CommandOnPath -Name git
if (-not $SkipReleases -or $IncludeCiArtifact) {
    Test-CommandOnPath -Name gh
}

$httpsUrl = "https://github.com/$GithubRepo.git"

if ($BareMirror -and -not $CloneTo) {
    throw "-BareMirror requires -CloneTo (path for the bare mirror clone)."
}

$workDir = $null
if ($CloneTo) {
    $clonePath = $CloneTo
    if (-not [System.IO.Path]::IsPathRooted($clonePath)) {
        $clonePath = Join-Path (Get-Location) $CloneTo
    }
    if (Test-Path $clonePath) {
        Write-Host "Using existing clone at $clonePath" -ForegroundColor DarkGray
        $workDir = $clonePath
    }
    else {
        Write-Host "Cloning $httpsUrl -> $clonePath" -ForegroundColor Yellow
        if (-not $DryRun) {
            $parent = Split-Path -Parent $clonePath
            if ($parent -and -not (Test-Path $parent)) {
                New-Item -ItemType Directory -Force -Path $parent | Out-Null
            }
            if ($BareMirror) {
                git clone --mirror $httpsUrl $clonePath
            }
            else {
                git clone --origin origin $httpsUrl $clonePath
            }
            if ($LASTEXITCODE -ne 0) { throw "git clone failed." }
        }
        $workDir = $clonePath
    }
}
else {
    $workDir = Get-RepoRoot
}

if (-not $BareMirror) {
    Invoke-GitFetchAll -WorkDir $workDir
}
else {
    Write-Host "Updating bare mirror $workDir ..." -ForegroundColor Yellow
    if (-not $DryRun) {
        Push-Location $workDir
        try {
            git remote update --prune
            if ($LASTEXITCODE -ne 0) { throw "git remote update --prune failed." }
        }
        finally {
            Pop-Location
        }
    }
}

$outRoot = $OutDir
if (-not [System.IO.Path]::IsPathRooted($outRoot)) {
    $base = if ($BareMirror) { Split-Path -Parent $workDir } else { $workDir }
    if (-not $base) { $base = Get-Location }
    $outRoot = Join-Path $base $OutDir
}

if (-not $SkipReleases) {
    Write-Host "Listing GitHub Releases for $GithubRepo ..." -ForegroundColor Yellow
    $releaseJson = gh release list -R $GithubRepo --limit 100 --json tagName,name,isLatest
    if ($LASTEXITCODE -ne 0) { throw "gh release list failed. Run 'gh auth login'." }
    $releases = $releaseJson | ConvertFrom-Json
    if ($Tags.Count -gt 0) {
        $releases = @($releases | Where-Object { $Tags -contains $_.tagName })
    }
    if (-not $releases -or @($releases).Count -eq 0) {
        Write-Warning "No matching releases found."
    }
    else {
        $relDir = Join-Path $outRoot "releases"
        foreach ($rel in @($releases)) {
            $tag = $rel.tagName
            $dest = Join-Path $relDir $tag
            Write-Host "  Release $tag -> $dest" -ForegroundColor White
            if ($DryRun) { continue }
            New-Item -ItemType Directory -Force -Path $dest | Out-Null
            gh release download $tag -R $GithubRepo -D $dest --skip-existing
            if ($LASTEXITCODE -ne 0) {
                throw "gh release download failed for $tag."
            }
        }
    }
}

if ($IncludeCiArtifact) {
    Write-Host "Finding newest successful main Build run ..." -ForegroundColor Yellow
    $runsJson = gh run list -R $GithubRepo --workflow dotnet.yml --branch main --status success --limit 5 --json databaseId,url,headSha,displayTitle,conclusion
    if ($LASTEXITCODE -ne 0) { throw "gh run list failed." }
    $runs = @($runsJson | ConvertFrom-Json)
    $run = $null
    foreach ($candidate in $runs) {
        $run = $candidate
        break
    }
    if (-not $run) {
        Write-Warning "No successful main Build runs found."
    }
    else {
        $ciDir = Join-Path $outRoot "ci"
        Write-Host "  CI run $($run.databaseId) ($($run.displayTitle))" -ForegroundColor White
        Write-Host "  $($run.url)" -ForegroundColor DarkGray
        Write-Host "  -> $ciDir" -ForegroundColor White
        if (-not $DryRun) {
            New-Item -ItemType Directory -Force -Path $ciDir | Out-Null
            gh run download $run.databaseId -R $GithubRepo -D $ciDir
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "gh run download failed for run $($run.databaseId) (artifact may have expired; GitHub keeps Actions artifacts ~30 days)."
            }
        }
    }
}

Write-Host ""
Write-Host "Done. Local git copy: $workDir" -ForegroundColor Green
if (-not $SkipReleases -or $IncludeCiArtifact) {
    Write-Host "Downloaded extras: $outRoot (gitignored; do not commit ZIPs)" -ForegroundColor Green
}
Write-Host "To rebuild current binaries from this source instead of downloading them: .\create-release-package.ps1" -ForegroundColor DarkGray
