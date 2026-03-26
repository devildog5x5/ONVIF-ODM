# Ensures single-file Windows publishes have a .exe apphost name. Some toolchains or zips
# occasionally leave the PE host as "AssemblyName" with no extension; Windows Explorer
# then won't treat it as an executable until the user renames it.
param(
    [Parameter(Mandatory)][string]$PublishDir,
    [Parameter(Mandatory)][string]$ExeBaseName
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $PublishDir)) {
    throw "Publish directory not found: $PublishDir"
}

$exe = Join-Path $PublishDir "$ExeBaseName.exe"
$bare = Join-Path $PublishDir $ExeBaseName

if (Test-Path $exe) {
    exit 0
}

if (Test-Path -LiteralPath $bare) {
    $item = Get-Item -LiteralPath $bare
    if ($item.PSIsContainer) {
        throw "Expected apphost file but found a directory: $bare"
    }
    Write-Warning "Windows apphost had no .exe extension; renaming '$ExeBaseName' -> '$ExeBaseName.exe'"
    Rename-Item -LiteralPath $bare -NewName "$ExeBaseName.exe"
    exit 0
}

throw "Windows publish is missing the apphost. Expected '$ExeBaseName.exe' (or extensionless '$ExeBaseName') in: $PublishDir"
