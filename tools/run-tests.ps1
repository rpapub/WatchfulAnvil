# Run tests for specific project or all tests
param(
    [string]$ProjectName = "CPRIMA.WorkflowAnalyzerRules.Tests"
)

Write-Host "Running tests for $ProjectName..."

# Get the root directory (where this script is run from)
$rootDir = $PSScriptRoot | Split-Path -Parent

# Build and run tests
$testProject = Join-Path $rootDir "tests\$ProjectName"

# Run tests with detailed output
dotnet test $testProject `
    --logger "console;verbosity=detailed" `
    --collect:"XPlat Code Coverage" `
    --results-directory "./TestResults"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Tests completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Tests failed!" -ForegroundColor Red
    exit 1
}
