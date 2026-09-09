<#
.SYNOPSIS
    Fails if a produced package declares a WatchfulAnvil dependency that is not being
    shipped alongside it.

.DESCRIPTION
    Rule packs are no longer self-contained: dropping ILRepack means each pack declares
    a dependency on WatchfulAnvil.Sdk instead of merging it in. That is only safe if the
    SDK reaches every feed its dependants reach.

    The failure this guards against is silent and has already happened: the packs that
    never ILRepacked were publishing a WatchfulAnvil.Sdk dependency that existed on no
    feed at all. Nothing failed at pack or push time; consumers just got an unresolvable
    dependency.

    So this asserts closure over the output directory: for every <dependency id="WatchfulAnvil.*">
    in every produced nuspec, a matching nupkg must be present in the same directory.

.PARAMETER Path
    Directory containing the produced .nupkg files. Defaults to ./nupkg.

.EXAMPLE
    pwsh scripts/Test-PackageClosure.ps1
    pwsh scripts/Test-PackageClosure.ps1 -Path artifacts
#>
[CmdletBinding()]
param(
    [string]$Path = "nupkg"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Test-Path $Path)) {
    Write-Error "Package directory not found: $Path"
    exit 2
}

$packages = Get-ChildItem -Path $Path -Filter *.nupkg -File |
    Where-Object { $_.FullName -notmatch '\\unpacked\\' }

if (-not $packages) {
    Write-Host "No .nupkg files in '$Path' - nothing to check."
    exit 0
}

# id/version pairs actually present, keyed case-insensitively as NuGet ids are.
$present = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

function Get-Nuspec {
    param([string]$NupkgPath)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' -and $_.FullName -notlike '*/*' } | Select-Object -First 1
        if (-not $entry) { return $null }
        $reader = New-Object System.IO.StreamReader($entry.Open())
        try { return [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
    }
    finally { $zip.Dispose() }
}

$specs = @{}
foreach ($pkg in $packages) {
    $xml = Get-Nuspec -NupkgPath $pkg.FullName
    if (-not $xml) {
        Write-Warning "No .nuspec inside $($pkg.Name); skipping."
        continue
    }
    $id = $xml.package.metadata.id
    $version = $xml.package.metadata.version
    [void]$present.Add("$id/$version")
    $specs[$pkg.Name] = $xml
}

$problems = @()

foreach ($name in ($specs.Keys | Sort-Object)) {
    $xml = $specs[$name]
    $consumerId = $xml.package.metadata.id

    # Dependencies appear either directly or nested in per-TFM <group> elements.
    $deps = @($xml.package.metadata.dependencies.dependency) +
            @($xml.package.metadata.dependencies.group.dependency)

    foreach ($dep in $deps | Where-Object { $_ -and $_.id -like 'WatchfulAnvil.*' }) {
        # A nuspec version may be a range: [1.0.0] exact, or a bare minimum.
        $wanted = $dep.version -replace '^\[|\]$', ''
        if ($present.Contains("$($dep.id)/$wanted")) { continue }

        $key = "$consumerId -> $($dep.id) $wanted"
        if ($problems -notcontains $key) { $problems += $key }
    }
}

if ($problems.Count -gt 0) {
    Write-Host ""
    Write-Host "Dependency closure FAILED in '$Path':" -ForegroundColor Red
    foreach ($p in $problems) {
        Write-Host "  $p  -- dependency declared but no matching .nupkg alongside it" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Pack the missing package into the same directory before publishing," -ForegroundColor Yellow
    Write-Host "or consumers will restore a dependency that resolves nowhere." -ForegroundColor Yellow
    exit 1
}

Write-Host "Dependency closure OK - $($packages.Count) package(s) in '$Path', every WatchfulAnvil dependency is shipped alongside." -ForegroundColor Green
exit 0
