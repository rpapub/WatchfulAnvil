<#
.SYNOPSIS
    Fails if an SDK type is re-declared outside WatchfulAnvil.Sdk.

.DESCRIPTION
    The SDK was forked wholesale once already: rpapub/dotnet-new-template vendored seven
    files from Core/ and Common/ with the namespace rewritten. The copy then drifted in
    BOTH directions and stranded two real bug fixes on the fork side for months.

    "Please don't copy the SDK" is not a strategy - it had already failed. This is the
    check that would have caught it on the day it happened.

    Matches DECLARATIONS, not usage. A plain substring search for "WorkflowRule" fires on
    every legitimate ": WorkflowRule" derivation and is therefore useless. The type names
    are derived at runtime from the SDK's own file names, so the list cannot go stale as
    the SDK grows.

.PARAMETER SdkPath
    The one directory allowed to declare these types.

.PARAMETER SearchPath
    Roots to scan. Defaults to the repository root.

.EXAMPLE
    pwsh scripts/Test-NoVendoredSdk.ps1
#>
[CmdletBinding()]
param(
    [string]$SdkPath = "src/WatchfulAnvil.Sdk",
    [string[]]$SearchPath = @(".")
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $SdkPath)) {
    Write-Error "SDK path not found: $SdkPath"
    exit 2
}

$sdkFull = (Resolve-Path $SdkPath).Path

# Type names to protect = the SDK's own source file names. Derived rather than hardcoded
# so a new SDK type is covered automatically.
$typeNames = Get-ChildItem -Path $sdkFull -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) } |
    Sort-Object -Unique

if (-not $typeNames) {
    Write-Error "No .cs files under $SdkPath - refusing to run with an empty type list."
    exit 2
}

$alternation = ($typeNames | ForEach-Object { [regex]::Escape($_) }) -join '|'
# Declaration only: optional modifiers, then class/interface/record, then the name,
# then a word boundary so RuleBase does not match RuleBaseHelper.
$pattern = "^\s*(?:public|internal|private|protected)?\s*(?:static\s+|abstract\s+|sealed\s+|partial\s+)*(?:class|interface|record|struct)\s+($alternation)\b"

$candidates = foreach ($root in $SearchPath) {
    Get-ChildItem -Path $root -Recurse -Filter *.cs -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -notmatch '\\(bin|obj|\.git|\.vs)\\' -and
            -not $_.FullName.StartsWith($sdkFull, [StringComparison]::OrdinalIgnoreCase)
        }
}

$hits = @()
foreach ($file in $candidates) {
    $lineNo = 0
    foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
        $lineNo++
        $m = [regex]::Match($line, $pattern)
        if ($m.Success) {
            $hits += [pscustomobject]@{
                File = Resolve-Path -Relative $file.FullName
                Line = $lineNo
                Type = $m.Groups[1].Value
                Text = $line.Trim()
            }
        }
    }
}

if ($hits.Count -gt 0) {
    Write-Host ""
    Write-Host "Vendored SDK types FOUND - these are declared outside $SdkPath :" -ForegroundColor Red
    foreach ($h in $hits) {
        Write-Host ("  {0}:{1}  {2}" -f $h.File, $h.Line, $h.Text) -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Reference WatchfulAnvil.Sdk instead of copying it. The last fork drifted in" -ForegroundColor Yellow
    Write-Host "both directions and stranded bug fixes on the copy for months." -ForegroundColor Yellow
    exit 1
}

Write-Host "No vendored SDK types - $($typeNames.Count) protected type name(s) checked across $($candidates.Count) file(s)." -ForegroundColor Green
exit 0
