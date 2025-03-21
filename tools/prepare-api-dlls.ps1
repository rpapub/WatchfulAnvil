# prepare-api-dlls.ps1
param()

# Get path of this script, resolve relative paths from here
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "../src/CPRIMA.WorkflowAnalyzerRules" | Resolve-Path -ErrorAction Stop

# Where to put temp NuGet downloads
$TempDir = Join-Path $ProjectDir "temp-nugets"
# Where to copy extracted DLLs
$OutDir = Join-Path $ProjectDir "lib-deps/UiPath.Activities.Api"

# Target versions and frameworks
$Targets = @(
    @{ Version = "23.10.3"; TFM = "net461" },
    @{ Version = "24.10.1"; TFM = "net6.0"  },
    @{ Version = "24.10.1"; TFM = "net8.0"  }
)

# Create temp dir
New-Item -ItemType Directory -Force -Path $TempDir | Out-Null

foreach ($target in $Targets) {
    $pkgId = "UiPath.Activities.Api"
    $pkgVersion = $target.Version
    $tfm = $target.TFM

    $pkgDir = Join-Path $TempDir "$pkgId.$pkgVersion"
    $dllSource = Join-Path $pkgDir "lib/$tfm/UiPath.Studio.Activities.Api.dll"
    $dllTargetDir = Join-Path $OutDir $tfm
    $dllTarget = Join-Path $dllTargetDir "UiPath.Studio.Activities.Api.dll"

    if (-Not (Test-Path $pkgDir)) {
        Write-Host "📦 Downloading $pkgId $pkgVersion..."
        dotnet nuget add source "https://uipath.pkgs.visualstudio.com/Public.Feeds/_packaging/UiPath-Official/nuget/v3/index.json" --name UiPath-Official -q
        nuget install $pkgId -Version $pkgVersion -OutputDirectory $TempDir -Source "UiPath-Official"
    }

    if (-Not (Test-Path $dllSource)) {
        throw "❌ DLL not found at: $dllSource"
    }

    New-Item -ItemType Directory -Force -Path $dllTargetDir | Out-Null
    Copy-Item -Path $dllSource -Destination $dllTarget -Force

    Write-Host "✅ Copied $tfm DLL to $dllTargetDir"
}

Write-Host "`nAll done."
