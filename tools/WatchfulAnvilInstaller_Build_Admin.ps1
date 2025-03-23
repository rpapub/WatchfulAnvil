# WatchfulAnvilInstaller_Build_Admin.ps1
# Builds the "Admin" mode installer from the repo root

$ErrorActionPreference = 'Stop'

# Resolve paths
$repoRoot = Resolve-Path "$PSScriptRoot/.."
$installerScript = Join-Path $repoRoot "installer\WatchfulAnvil_Installer_Admin.iss"
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "📦 Building ADMIN installer..."

# Check if ISCC exists
if (-not (Test-Path $isccPath)) {
    Write-Error "❌ ISCC.exe not found at '$isccPath'. Is Inno Setup installed?"
    exit 1
}

# Run ISCC from repo root with full path to .iss
Push-Location $repoRoot
& "$isccPath" "$installerScript"
Pop-Location

Write-Host "✅ Build complete."
