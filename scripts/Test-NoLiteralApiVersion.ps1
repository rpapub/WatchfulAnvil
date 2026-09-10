<#
.SYNOPSIS
    Fails if a project pins UiPath.Activities.Api to a literal version.

.DESCRIPTION
    The per-TFM version matrix belongs in build/WatchfulAnvil.Build/build/WatchfulAnvil.Build.props
    and nowhere else. It used to be duplicated across eleven csproj files, and it had
    already drifted: the scaffolding template pinned 24.10.1 for net6.0 where every real
    pack pinned 23.10.3, so anything generated from it bound the wrong API for Studio
    older than 2024.10. Three shipping packs had the same mismatch.

    Nothing failed - the drift was simply invisible. This makes it visible.

.PARAMETER SearchPath
    Roots to scan. Defaults to src/ and dist/.

.EXAMPLE
    pwsh scripts/Test-NoLiteralApiVersion.ps1
#>
[CmdletBinding()]
param(
    [string[]]$SearchPath = @("src", "dist")
)

$ErrorActionPreference = 'Stop'

# A literal version means digits. $(WatchfulAnvilApiVersion) is the intended form.
$pattern = 'UiPath\.Activities\.Api"\s+Version="\d'

$files = foreach ($root in $SearchPath) {
    if (-not (Test-Path $root)) { continue }
    Get-ChildItem -Path $root -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
}

$hits = @()
foreach ($file in $files) {
    $lineNo = 0
    foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
        $lineNo++
        if ($line -match $pattern) {
            $hits += [pscustomobject]@{
                File = Resolve-Path -Relative $file.FullName
                Line = $lineNo
                Text = $line.Trim()
            }
        }
    }
}

if ($hits.Count -gt 0) {
    Write-Host ""
    Write-Host "Literal UiPath.Activities.Api version(s) FOUND:" -ForegroundColor Red
    foreach ($h in $hits) {
        Write-Host ("  {0}:{1}  {2}" -f $h.File, $h.Line, $h.Text) -ForegroundColor Red
    }
    Write-Host ""
    Write-Host 'Use Version="$(WatchfulAnvilApiVersion)" and set WatchfulAnvilRulePack=true.' -ForegroundColor Yellow
    Write-Host "The per-TFM matrix lives in build/WatchfulAnvil.Build/build/WatchfulAnvil.Build.props." -ForegroundColor Yellow
    exit 1
}

Write-Host "No literal UiPath.Activities.Api versions - $($files.Count) project file(s) checked." -ForegroundColor Green
exit 0
