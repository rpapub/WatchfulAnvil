# WatchfulAnvilInstaller_GenerateVersion.ps1
# Generates version.generated.iss for Inno Setup from GitVersion metadata

$ErrorActionPreference = 'Stop'

# Determine root and validate structure
$repoRoot = Resolve-Path "$PSScriptRoot\.."
$expectedToolsDir = Join-Path $repoRoot "tools"
$expectedInstallerDir = Join-Path $repoRoot "installer"

# Validation: this script must reside in ./tools/
if (-not (Test-Path -Path $expectedToolsDir -PathType Container)) {
    Write-Error "Expected directory 'tools/' not found at: $expectedToolsDir"
    exit 1
}
if ($PSScriptRoot -ne $expectedToolsDir) {
    Write-Error "This script must be located in the 'tools/' directory of the repo."
    Write-Host "Current script path: $PSScriptRoot"
    exit 1
}

# Validation: installer folder must exist
if (-not (Test-Path -Path $expectedInstallerDir -PathType Container)) {
    Write-Error "Expected directory 'installer/' not found at: $expectedInstallerDir"
    exit 1
}

# Output path
$versionFile = Join-Path $expectedInstallerDir "version.generated.iss"

# Run GitVersion
Write-Host "Running GitVersion..."
try {
    $gitVersionJson = dotnet-gitversion -output json
    if (-not $gitVersionJson) {
        throw "GitVersion returned no output"
    }
    $gitVersion = $gitVersionJson | ConvertFrom-Json
} catch {
    Write-Error 'Failed to run GitVersion. Try: `dotnet tool install --global GitVersion.Tool`'
    exit 1
}

# Extract versions
$appVersion = $gitVersion.NuGetVersionV2
if (-not $appVersion -or $appVersion -eq 'null') {
    Write-Warning "⚠️ NuGetVersionV2 was null, falling back to FullSemVer."
    $appVersion = $gitVersion.FullSemVer
}

# Installer-wide metadata (should match main .csproj values, manually synced)
# TODO: When multiple rule projects exist, extract metadata from all ./src/*/*.csproj
#       Requires agreed conventions (e.g., single <PackageId>, consistent <Authors>, etc.)
#       For now, using hardcoded values until real-world complexity emerges
$appName        = "WatchfulAnvil.WorkflowAnalyzerRules"
$appPublisher   = "Christian Prior-Mamulyan"
$appCompany     = "cprima.net"
$appDescription = "Custom UiPath Workflow Analyzer rules by Christian Prior-Mamulyan."
$appUrl         = "https://github.com/rpapub/WatchfulAnvil"

# Build full output filenames
$outputBaseFilenameUser  = "Setup_${appName}_${appVersion}-User"
$outputBaseFilenameAdmin = "Setup_${appName}_${appVersion}-Admin"

# Write to version.generated.iss
Write-Host "Version determined: $appVersion"
Write-Host "Writing to: $versionFile"

# Emit Inno Setup definitions
$innoContent = @"
; This file is auto-generated. Do not edit manually.
#define MyAppName "$appName"
#define MyAppVersion "$appVersion"
#define MyAppPublisher "$appPublisher"
#define MyAppCompany "$appCompany"
#define MyAppDescription "$appDescription"
#define MyAppUrl "$appUrl"
#define OutputBaseFilename_User "$outputBaseFilenameUser"
#define OutputBaseFilename_Admin "$outputBaseFilenameAdmin"
"@

Set-Content -Encoding UTF8 -NoNewline -Path $versionFile -Value $innoContent

Write-Host "Done."
