param()

# Get path of this script, resolve relative paths from here
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "../src/CPRIMA.WorkflowAnalyzerRules" | Resolve-Path -ErrorAction Stop

$feedName = "UiPath-Official"
$feedUrl = "https://uipath.pkgs.visualstudio.com/Public.Feeds/_packaging/UiPath-Official/nuget/v3/index.json"

# Where to put temp NuGet downloads
$TempDir = Join-Path $ProjectDir "temp-nugets"
$TEMP_NUGET_DIR = Join-Path $ProjectDir "temp-nugets"
# Where to copy extracted DLLs
$OutDir = Join-Path $ProjectDir "lib-deps/UiPath.Activities.Api"

# Target versions and frameworks
$Targets = @(
    @{ Version = "23.10.3"; TFM = "net461" },
    @{ Version = "24.10.1"; TFM = "net6.0"  },
    @{ Version = "24.10.1"; TFM = "net8.0"  }
)

# Ensure feed source is added
if (-not (dotnet nuget list source | Select-String $feedUrl)) {
    dotnet nuget add source $feedUrl --name $feedName
}

# Create temp directory for nuget downloads
New-Item -ItemType Directory -Force -Path $TempDir | Out-Null

# Create a dummy project for restoring NuGet packages
$DummyDir = Join-Path $TempDir "dummy"
$TempProj = Join-Path $DummyDir "dummy.csproj"
if (-not (Test-Path $TempProj)) {
    dotnet new classlib -o $DummyDir --framework netstandard2.0 --force | Out-Null
}

foreach ($target in $Targets) {
    $pkgId = "UiPath.Activities.Api"
    $pkgVersion = $target.Version
    $tfm = $target.TFM

    Write-Host "📦 Restoring $pkgId $pkgVersion for $tfm..."

    $PkgProjDir = Join-Path $TempDir "pkg-$tfm"
    $PkgProj = Join-Path $PkgProjDir "pkg-$tfm.csproj"
    
    # Always generate with netstandard2.0 (safe across environments)
    dotnet new classlib --framework netstandard2.0 --output $PkgProjDir --name "pkg-$tfm" --force | Out-Null
    dotnet add $PkgProj package $pkgId --version $pkgVersion --source $feedUrl | Out-Null
    dotnet restore $PkgProj --source $feedUrl --packages $TempDir/packages | Out-Null

    # Extract DLLs from restored package
    $pkgDir = Join-Path $TempDir "packages/$($pkgId.ToLower())/$pkgVersion/lib/$tfm"
    $dllSource = Join-Path $pkgDir "UiPath.Studio.Activities.Api.dll"
    $dllTargetDir = Join-Path $OutDir $tfm

    if (-not (Test-Path $dllSource)) {
        throw "❌ DLL not found at: $dllSource"
    }

    # Create directory for the DLLs and copy them
    New-Item -ItemType Directory -Force -Path $dllTargetDir | Out-Null
    Copy-Item -Path $dllSource -Destination $dllTargetDir -Force
    Write-Host "✅ Copied $tfm DLL to $dllTargetDir"
}

# Clean up the temporary NuGet download directory after extraction
Write-Host "🧹 Cleaning up temporary NuGet directory: $TempDir"
Remove-Item -Recurse -Force $TempDir | Out-Null

Write-Host "`nAll done."
