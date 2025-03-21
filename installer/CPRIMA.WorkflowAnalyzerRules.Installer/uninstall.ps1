# uninstall.ps1
# Removes CPRIMA.WorkflowAnalyzerRules.dll from known UiPath Rules folders with debug output

$RuleName = "CPRIMA.WorkflowAnalyzerRules.dll"

$TargetRoots = @(
  "$env:ProgramFiles\UiPath\Studio",
  "$env:LocalAppData\Programs\UiPath\Studio"
)

$TargetSuffixes = @(
  "Rules\net8.0",
  "Rules\net6.0",
  "net461\Rules",
  "Rules" # legacy fallback
)

Write-Host "Starting CPRIMA Workflow Analyzer Rules uninstallation..."

foreach ($root in $TargetRoots) {
  foreach ($suffix in $TargetSuffixes) {
    $targetPath = Join-Path $root $suffix
    $filePath = Join-Path $targetPath $RuleName

    Write-Host "Checking path: $filePath"

    if (Test-Path $filePath) {
      try {
        Remove-Item -Path $filePath -Force
        Write-Host "  Removed: $filePath"
      } catch {
        Write-Host "  ERROR: Failed to remove $filePath — $_"
      }
    } else {
      Write-Host "  Not found: $filePath"
    }
  }
}

Write-Host "Uninstallation complete."