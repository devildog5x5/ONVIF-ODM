#Requires -Version 5.1
<#
.SYNOPSIS
  Regenerates devildog5x5 profile repo README.md from GitHub Releases (gh API).

.DESCRIPTION
  Uses the latest release per configured repo and builds direct releases/download URLs.
  Edit $Sections below when you add projects or rename assets.

.PARAMETER Owner
  GitHub user or org (default: devildog5x5).

.PARAMETER ProfileReadmePath
  Path to README.md in the profile repo. Default: sibling folder devildog5x5 next to ONVIF-ODM.

.PARAMETER DryRun
  Print README to stdout only; do not write a file.

.PARAMETER Commit
  After writing, git add/commit/push in the profile repo (requires clean intent + gh auth).

.EXAMPLE
  .\scripts\update-profile-downloads-readme.ps1

.EXAMPLE
  .\scripts\update-profile-downloads-readme.ps1 -ProfileReadmePath C:\path\to\devildog5x5\README.md -Commit
#>
[CmdletBinding()]
param(
    [string] $Owner = "devildog5x5",
    [string] $ProfileReadmePath = "",
    [switch] $DryRun,
    [switch] $Commit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$emDash = [char]0x2014

function Test-GhCli {
    $null = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $?) {
        throw "GitHub CLI 'gh' not found on PATH. Install from https://cli.github.com/ and run 'gh auth login'."
    }
}

function Get-LatestRelease {
    param([string] $Repo)
    $uri = "repos/$Owner/$Repo/releases/latest"
    $raw = gh api $uri 2>&1
    if ($LASTEXITCODE -ne 0) { return $null }
    return ($raw | ConvertFrom-Json)
}

function Find-AssetName {
    param(
        [object[]] $Assets,
        [string] $Exact,
        [string] $NameLike
    )
    if ($Exact) {
        $m = $Assets | Where-Object { $_.name -ceq $Exact }
        if ($m) { return $m[0].name }
    }
    if ($NameLike) {
        $m = $Assets | Where-Object { $_.name -like $NameLike }
        if ($m) { return $m[0].name }
    }
    return $null
}

# --- Edit this block when adding repos or changing which assets appear on the profile page ---
$Sections = @(
    @{
        Title       = "ONVIF Device Manager"
        Repo        = "ONVIF-ODM"
        Description = "ONVIF IP camera discovery, live RTSP video, PTZ control, snapshots, and event simulation. Cross-platform (Windows, Linux, macOS)."
        TableHeader = @("Platform", "Download")
        Rows        = @(
            @{ Label = "Windows x64"; Like = "*Avalonia-win-x64*.zip" }
            @{ Label = "Linux x64"; Like = "*Avalonia-linux-x64*.zip" }
            @{ Label = "macOS Intel"; Like = "*Avalonia-osx-x64*.zip" }
            @{ Label = "macOS Apple Silicon"; Like = "*Avalonia-osx-arm64*.zip" }
        )
        Footer      = @"
Latest CI (Windows only, may be newer than Release): [Actions artifacts](https://github.com/$Owner/ONVIF-ODM/actions/workflows/dotnet.yml?query=branch%3Amain) | [All releases](https://github.com/$Owner/ONVIF-ODM/releases) | [Source](https://github.com/$Owner/ONVIF-ODM)
"@
    }
    @{
        Title       = "PTZ Camera Operator"
        Repo        = "PTZ_Interface"
        Description = "PTZ camera control interface for ONVIF-compatible IP cameras."
        TableHeader = @("Download", "Link")
        Rows        = @(
            @{ Label = "Portable ZIP"; Exact = "PTZCameraOperator-Portable.zip" }
            @{ Label = "Windows Installer"; Exact = "PTZCameraOperatorSetup.exe" }
        )
        Footer      = @"
[All releases](https://github.com/$Owner/PTZ_Interface/releases) | [Source](https://github.com/$Owner/PTZ_Interface)
"@
    }
    @{
        Title       = "IP Management Interface"
        Repo        = "IPManagementInterface"
        Description = "Network device management tool."
        TableHeader = @("Download", "Link")
        Rows        = @(
            @{ Label = "Windows Installer (MSI)"; Exact = "IPManagementInterface-Setup.msi" }
            @{ Label = "Portable EXE"; Exact = "IPManagementInterface.exe" }
        )
        Footer      = @"
[All releases](https://github.com/$Owner/IPManagementInterface/releases) | [Source](https://github.com/$Owner/IPManagementInterface)
"@
    }
    @{
        Title       = "Video Editor"
        Repo        = "Video_Editor"
        Description = "Professional video editor $emDash available in C# (WPF/.NET 8) and Python (PyQt6) editions."
        TableHeader = @("Download", "Link")
        Rows        = @(
            @{ Label = "C# Windows Installer"; Exact = "VideoEditor-CSharp-Setup.exe" }
            @{ Label = "Python Windows Installer"; Exact = "VideoEditor-Python-Setup.exe" }
            @{ Label = "Portable EXE"; Exact = "VideoEditor.exe" }
        )
        Footer      = @"
[All releases](https://github.com/$Owner/Video_Editor/releases) | [C# source](https://github.com/$Owner/VideoEditor-CSharp) | [Python source](https://github.com/$Owner/VideoEditor-Python)
"@
    }
)

$OtherReposMarkdown = @"
## Other Repositories

| Project | Description | Link |
|---------|-------------|------|
| PTZ Camera Operator Downloads | Download page | [Repo](https://github.com/$Owner/PTZCameraOperator-Downloads) |
| TreeBuilder Modern | TreeBuilder as a security tool | [Source](https://github.com/$Owner/TreeBuilderModern) |
| Clippit | Clip it for Windows | [Source](https://github.com/$Owner/Clippit) |
| TestEnvironmentBuilder | Test environment builder | [Source](https://github.com/$Owner/TestEnvironmentBuilder) |

---
"@

Test-GhCli

if (-not $ProfileReadmePath) {
    $onvifRoot = Split-Path -Parent $PSScriptRoot
    $githubRoot = Split-Path -Parent $onvifRoot
    $ProfileReadmePath = Join-Path $githubRoot "devildog5x5\README.md"
}

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Robert Foster $emDash Software Downloads")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("Central download page for all published projects. Each link goes directly to the latest release asset on GitHub.")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("---")
[void]$sb.AppendLine("")

$failures = New-Object System.Collections.Generic.List[string]

foreach ($sec in $Sections) {
    $rel = Get-LatestRelease -Repo $sec.Repo
    if (-not $rel) {
        [void]$failures.Add("$($sec.Repo): no 'latest' release (create a release or fix repo name).")
        continue
    }
    $tag = $rel.tag_name
    $assets = @($rel.assets)

    [void]$sb.AppendLine("## $($sec.Title)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine($sec.Description)
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("| $($sec.TableHeader[0]) | $($sec.TableHeader[1]) |")
    [void]$sb.AppendLine("|----------|----------|")

    foreach ($row in $sec.Rows) {
        $exact = $row['Exact']
        $like = $row['Like']
        $name = Find-AssetName -Assets $assets -Exact $exact -NameLike $like
        if (-not $name) {
            $hint = if ($exact) { $exact } else { $like }
            [void]$failures.Add("$($sec.Repo): asset not found ($hint).")
            continue
        }
        $url = "https://github.com/$Owner/$($sec.Repo)/releases/download/$tag/$name"
        $col1 = $row['Label']
        $col2 = "[$name]($url)"
        if ($sec.TableHeader[0] -eq "Platform") {
            [void]$sb.AppendLine("| $col1 | $col2 |")
        } else {
            [void]$sb.AppendLine("| $col1 | $col2 |")
        }
    }

    [void]$sb.AppendLine("")
    [void]$sb.AppendLine(($sec.Footer -replace "`r`n", "`n").TrimEnd())
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("")
}

[void]$sb.AppendLine(($OtherReposMarkdown -replace "`r`n", "`n").TrimEnd())
[void]$sb.AppendLine("")
$stamp = Get-Date -Format "yyyy-MM-dd"
[void]$sb.AppendLine("*Last updated: $stamp*")

$text = $sb.ToString().TrimEnd() + "`n"

if ($failures.Count -gt 0) {
    Write-Warning "Some rows were skipped:"
    $failures | ForEach-Object { Write-Warning "  $_" }
}

if ($DryRun) {
    Write-Output $text
    exit 0
}

$dir = Split-Path -Parent $ProfileReadmePath
if (-not (Test-Path -LiteralPath $dir)) {
    throw "Profile repo folder not found: $dir. Clone devildog5x5 or pass -ProfileReadmePath."
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($ProfileReadmePath, $text, $utf8NoBom)
Write-Host "Wrote $ProfileReadmePath"

if ($Commit) {
    Push-Location $dir
    try {
        git add README.md
        git commit -m "Refresh download links from latest GitHub releases"
        git push
    } finally {
        Pop-Location
    }
}
