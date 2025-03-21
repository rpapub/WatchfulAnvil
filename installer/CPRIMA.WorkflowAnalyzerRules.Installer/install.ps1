# install.ps1
# Deploys CPRIMA.WorkflowAnalyzerRules.dll to existing UiPath Rules folders with debug output

$RuleName = "CPRIMA.WorkflowAnalyzerRules.dll"
$SourcePaths = @{
  "net8.0"  = Join-Path $PSScriptRoot "net8.0\$RuleName"
  "net6.0"  = Join-Path $PSScriptRoot "net6.0\$RuleName"
  "net461"  = Join-Path $PSScriptRoot "net461\$RuleName"
}

$TargetRoots = @()

if ($env:ProgramW6432) {
    $TargetRoots += Join-Path $env:ProgramW6432 "UiPath\Studio"
}
if ($env:ProgramFiles) {
    $TargetRoots += Join-Path $env:ProgramFiles "UiPath\Studio"
}
if ($env:ProgramFilesX86) {
    $TargetRoots += Join-Path $env:ProgramFilesX86 "UiPath\Studio"
}
if ($env:LocalAppData) {
    $TargetRoots += Join-Path $env:LocalAppData "Programs\UiPath\Studio"
}

$TargetSuffixes = @(
  "Rules\net8.0",
  "Rules\net6.0",
  "net461\Rules",
  "Rules" # legacy fallback
)

Write-Host "Starting CPRIMA Workflow Analyzer Rules installation..."
Write-Host "Script location: $PSScriptRoot"

foreach ($root in $TargetRoots) {
  foreach ($suffix in $TargetSuffixes) {
    $targetPath = Join-Path $root $suffix
    $framework = Split-Path $suffix -Leaf

    Write-Host "Checking path: $targetPath"

    if (Test-Path $targetPath) {
      if ($SourcePaths.ContainsKey($framework)) {
        $sourceFile = $SourcePaths[$framework]
        if (Test-Path $sourceFile) {
          $destination = Join-Path $targetPath $RuleName
          Write-Host "  [SIMULATION] Would copy $framework -> $destination"
        } else {
          Write-Host "  WARNING: Source file not found: $sourceFile"
        }
      } else {
        Write-Host "  No source mapping for framework: $framework"
      }
    } else {
      Write-Host "  Skipping: Folder does not exist"
    }
  }
}

Write-Host "Installation complete."
