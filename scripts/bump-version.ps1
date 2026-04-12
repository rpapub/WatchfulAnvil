<#
.SYNOPSIS
    Bumps the version in Cpmf.WorkflowAnalyzerRules.csproj and copies the freshly-packed
    nupkg to the local NuGet feed.

.PARAMETER Part
    Which part to increment: Patch (default), Minor, or Major.
    Bumping Minor resets Patch to 0; bumping Major resets Minor and Patch to 0.

.PARAMETER Suffix
    Pre-release suffix to append (default: "alpha").  Pass "" to produce a release build.

.PARAMETER NoPack
    Skip packing the project after bumping (pack runs by default).

.PARAMETER LocalFeed
    Destination for the packed nupkg. Default: C:\Users\Public\Documents\myNugetPackages

.EXAMPLE
    .\scripts\bump-version.ps1                   # patch bump, then pack and copy to local feed
    .\scripts\bump-version.ps1 -Part Minor        # minor bump -> 0.1.13-alpha -> 0.2.0-alpha
    .\scripts\bump-version.ps1 -Suffix ""         # patch bump, no suffix -> 0.1.14
    .\scripts\bump-version.ps1 -NoPack            # bump only, skip pack
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet("Major","Minor","Patch")]
    [string] $Part = "Patch",

    [string] $Suffix = "alpha",

    [switch] $NoPack,

    [string] $LocalFeed = "C:\Users\Public\Documents\myNugetPackages"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot  = Split-Path $PSScriptRoot -Parent
$CsprojPath = Join-Path $RepoRoot "src\Cpmf.WorkflowAnalyzerRules\Cpmf.WorkflowAnalyzerRules.csproj"

if (-not (Test-Path $CsprojPath)) {
    throw "csproj not found at: $CsprojPath"
}

# ── Read current version ─────────────────────────────────────────────────────

[xml]$csproj = Get-Content $CsprojPath -Raw
$current = $csproj.SelectSingleNode("//Version").'#text'

if (-not $current) {
    throw "Could not find <Version> element in $CsprojPath"
}

# Strip pre-release suffix (everything after the first '-')
$corePart   = ($current -split '-')[0]
$semver     = $corePart -split '\.'
if ($semver.Count -ne 3) {
    throw "Unexpected version format '$current' - expected Major.Minor.Patch[-suffix]"
}

[int]$major = $semver[0]
[int]$minor = $semver[1]
[int]$patch = $semver[2]

# ── Calculate new version ────────────────────────────────────────────────────

switch ($Part) {
    "Major" { $major++; $minor = 0; $patch = 0 }
    "Minor" { $minor++; $patch = 0 }
    "Patch" { $patch++ }
}

$newCore    = "$major.$minor.$patch"
$newVersion = if ($Suffix) { "$newCore-$Suffix" } else { $newCore }

Write-Host "Bumping ${Part}: $current  ->  $newVersion"

# ── Patch all four version fields ────────────────────────────────────────────

$raw = Get-Content $CsprojPath -Raw
$raw = $raw -replace '(<Version>)[^<]+(</Version>)',               ('${1}' + $newVersion + '${2}')
$raw = $raw -replace '(<FileVersion>)[^<]+(</FileVersion>)',       ('${1}' + $newCore + '.0${2}')
$raw = $raw -replace '(<AssemblyVersion>)[^<]+(</AssemblyVersion>)',('${1}' + $newCore + '.0${2}')
$raw = $raw -replace '(<InformationalVersion>)[^<]+(</InformationalVersion>)',('${1}' + $newVersion + '${2}')

if ($PSCmdlet.ShouldProcess($CsprojPath, "Update version to $newVersion")) {
    Set-Content -Path $CsprojPath -Value $raw -NoNewline:$false
    Write-Host "Updated $CsprojPath"
}

# ── Pack ─────────────────────────────────────────────────────────────────────

if (-not $NoPack) {
    $artifactsDir = Join-Path $RepoRoot "artifacts"
    Write-Host "Packing…"
    dotnet pack $CsprojPath -c Release -o $artifactsDir --nologo /p:Version=$newVersion /p:FileVersion="$newCore.0" /p:AssemblyVersion="$newCore.0" /p:InformationalVersion=$newVersion
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed (exit $LASTEXITCODE)" }

    # Copy to local feed
    if (Test-Path $LocalFeed) {
        $nupkg = Join-Path $artifactsDir "Cpmf.WorkflowAnalyzerRules.$newVersion.nupkg"
        if (Test-Path $nupkg) {
            Copy-Item $nupkg $LocalFeed -Force
            Write-Host "Copied $nupkg  ->  $LocalFeed"
        } else {
            Write-Warning "nupkg not found at expected path: $nupkg"
        }
    } else {
        Write-Warning "Local feed directory not found: $LocalFeed - skipping copy."
    }
}

Write-Host ""
Write-Host "Done. New version: $newVersion"
