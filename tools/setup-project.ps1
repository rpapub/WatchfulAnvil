$ErrorActionPreference = 'Stop'

# --- Template Availability Check ---
$templatesOk = @{
  RegisterAnalyzerConfiguration = Test-Path ".\templates\workflow-analyzer-rule\RegisterAnalyzerConfiguration.cs"
  ProjectFileTemplate           = Test-Path ".\templates\workflow-analyzer-rule\Project.csproj"
  LibDepsFolder                 = Test-Path ".\templates\workflow-analyzer-rule\lib-deps\"
  SampleRule                    = Test-Path ".\templates\workflow-analyzer-rule\Rules\SampleRule.cs"
}

if ($templatesOk.Values -contains $false) {
    Write-Error "One or more required template files are missing. Aborting."
    $templatesOk.GetEnumerator() | ForEach-Object {
        if ($_.Value) {
            Write-Host ("{0,-30} ✅" -f $_.Key)
        } else {
            Write-Host ("{0,-30} ❌" -f $_.Key)
        }
    }
    exit 1
}

Write-Host ""
Write-Host "Starting project setup..."

# --- Hardcoded Project Base Name (short) ---
$ProjectBaseName = "HelloWorld"

# --- Input with Defaults ---
$defaultOrg       = "ACME"
$defaultRulePrefix = "HWD"

$Organization = Read-Host "Enter your organization name [$defaultOrg]"
if ([string]::IsNullOrWhiteSpace($Organization)) { $Organization = $defaultOrg }

$RulePrefix = Read-Host "Enter the rule ID prefix (2-5 uppercase letters) [$defaultRulePrefix]"
if ([string]::IsNullOrWhiteSpace($RulePrefix)) { $RulePrefix = $defaultRulePrefix }

# --- Input Validation ---
if ($RulePrefix -notmatch '^[A-Z]{2,5}$') {
    Write-Error "Rule prefix must be 2-5 uppercase letters."
    exit 1
}

# --- Derived Values ---
$ProjectName     = "$ProjectBaseName.WorkflowAnalyzerRules"
$Namespace       = "$Organization.$ProjectName"
$TestProjectName = "$ProjectName.Tests"

# --- Determine Root Directory ---
$ScriptDir = $PSScriptRoot
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $RepoRoot
Write-Host "Working directory set to: $RepoRoot"

# --- Paths ---
$SrcPath       = Join-Path $RepoRoot "src\$ProjectName"
$TestPath      = Join-Path $RepoRoot "tests\$TestProjectName"
$ProjFile      = Join-Path $SrcPath "$ProjectName.csproj"
$TestProjFile  = Join-Path $TestPath "$TestProjectName.csproj"

# --- Error: Existing project directory ---
if ((Test-Path $SrcPath) -or (Test-Path $TestPath)) {
    Write-Error "Project or test directory already exists. Aborting."
    exit 1
}

# --- Error: Project files already exist ---
if ((Test-Path $ProjFile) -or (Test-Path $TestProjFile)) {
    Write-Error "One or more target project files already exist. Aborting."
    exit 1
}

# --- Warning: Rule prefix conflict not auto-checked ---
Write-Host ""
Write-Host "NOTE: Please manually ensure no existing rules use prefix '$RulePrefix' (e.g. ${RulePrefix}0001)."
Write-Host ""

# --- DEBUG OUTPUT ---
Write-Host "`n--- Debug Info ---" -ForegroundColor Yellow
Write-Host "Organization:        $Organization"
Write-Host "RulePrefix:          $RulePrefix"
Write-Host "ProjectBaseName:     $ProjectBaseName"
Write-Host "ProjectName:         $ProjectName"
Write-Host "Namespace:           $Namespace"
Write-Host "TestProjectName:     $TestProjectName"
Write-Host "RepoRoot:            $RepoRoot"
Write-Host "SrcPath:             $SrcPath"
Write-Host "TestPath:            $TestPath"
Write-Host "ProjFile:            $ProjFile"
Write-Host "TestProjFile:        $TestProjFile"
Write-Host "-------------------`n"

# --- Dotnet Helper to Suppress Info ---
function Run-DotnetQuiet {
    param (
        [string]$cmd
    )
    Write-Host "→ dotnet $cmd" -ForegroundColor DarkGray
    $args = $cmd -split ' '
    & dotnet @args 2>&1 | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) {
            Write-Error $_
        }
    }
}

New-Item -ItemType Directory -Force -Path (Join-Path $SrcPath "Rules") | Out-Null

# --- Create Main Project ---
Run-DotnetQuiet "new classlib -n $ProjectName -o $SrcPath"
Remove-Item -Force (Join-Path $SrcPath "Class1.cs") -ErrorAction SilentlyContinue

# --- Create Test Project ---
Run-DotnetQuiet "new xunit -n $TestProjectName -o $TestPath --force"

# --- Add Projects to Solution ---
Run-DotnetQuiet "sln add $ProjFile"
Run-DotnetQuiet "sln add $TestProjFile"

# --- Add Reference from Test Project to Main Project ---
Run-DotnetQuiet "add $TestProjFile reference $ProjFile"

# --- Add Test Dependencies ---
Run-DotnetQuiet "add $TestProjFile package Moq"
Run-DotnetQuiet "add $TestProjFile package xunit"
Run-DotnetQuiet "add $TestProjFile package Microsoft.NET.Test.Sdk"
Run-DotnetQuiet "add $TestProjFile package xunit.runner.visualstudio"

# --- File Copy and Transformation Helpers ---
function Copy-And-ReplacePlaceholders {
    param (
        [string]$TemplatePath,
        [string]$TargetPath
    )

    $content = Get-Content $TemplatePath -Raw
    $content = $content -replace '{{NAMESPACE}}', $Namespace
    $content = $content -replace '{{PREFIX}}', $RulePrefix
    $content = $content -replace '{{ORGANIZATION}}', $Organization

    Set-Content -Path $TargetPath -Value $content
}

# --- Copy Over Templates ---
Copy-And-ReplacePlaceholders ".\templates\workflow-analyzer-rule\RegisterAnalyzerConfiguration.cs" (Join-Path $SrcPath "RegisterAnalyzerConfiguration.cs")
Copy-And-ReplacePlaceholders ".\templates\workflow-analyzer-rule\Project.csproj" $ProjFile
Copy-And-ReplacePlaceholders ".\templates\workflow-analyzer-rule\Rules\SampleRule.cs" (Join-Path $SrcPath "Rules\SampleRule.cs")

# --- Copy lib-deps ---
New-Item -ItemType Directory -Force -Path (Join-Path $SrcPath "lib-deps") | Out-Null
Copy-Item -Path ".\templates\workflow-analyzer-rule\lib-deps\*" `
          -Destination (Join-Path $SrcPath "lib-deps") `
          -Recurse -Force

Write-Host "`n📁 Project scaffolding completed."
Write-Host "✅ You can now open the solution and build the new project."
