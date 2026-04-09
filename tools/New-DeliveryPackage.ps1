#Requires -Version 5.1
<#
.SYNOPSIS
    Generates a delivery package project from a registry rules.yaml and a dist manifest.json.

.DESCRIPTION
    Reads registry/<OrgPrefix>/rules.yaml and dist/<PackageName>/manifest.json,
    then emits dist/<PackageName>/<PackageName>.csproj and
    dist/<PackageName>/RegisterAnalyzerConfiguration.g.cs.

.PARAMETER PackageName
    The delivery package name, e.g. Cpmf.Standard.
    The org prefix (first segment) is used to locate the registry YAML.

.EXAMPLE
    .\tools\New-DeliveryPackage.ps1 -PackageName Cpmf.Standard
#>
param(
    [Parameter(Mandatory)]
    [string] $PackageName
)

$ErrorActionPreference = 'Stop'
$repoRoot  = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$orgPrefix = $PackageName.Split('.')[0]

$registryPath  = Join-Path $repoRoot "registry\$orgPrefix\rules.yaml"
$distDir       = Join-Path $repoRoot "dist\$PackageName"
$manifestPath  = Join-Path $distDir  'manifest.json'
$csprojPath    = Join-Path $distDir  "$PackageName.csproj"
$bootstrapPath = Join-Path $distDir  'RegisterAnalyzerConfiguration.g.cs'

if (-not (Test-Path $registryPath))  { Write-Error "Registry not found: $registryPath";  exit 1 }
if (-not (Test-Path $manifestPath))  { Write-Error "Manifest not found: $manifestPath";   exit 1 }

# ── YAML parser ───────────────────────────────────────────────────────────────
# Handles the specific structure of rules.yaml (no anchors, no block scalars).

function ConvertFrom-RegistryYaml {
    param([string] $Path)

    $content = Get-Content $Path -Raw -Encoding UTF8
    $result  = @{ package = ''; namespace = ''; version = ''; rules = @() }

    # Top-level scalars
    if ($content -match '(?m)^package:\s+"?([^"\r\n]+)"?')   { $result.package   = $Matches[1].Trim() }
    if ($content -match '(?m)^namespace:\s+"?([^"\r\n]+)"?') { $result.namespace = $Matches[1].Trim() }
    if ($content -match '(?m)^version:\s+"?([^"\r\n]+)"?')   { $result.version   = $Matches[1].Trim() }

    # Rules section
    $rulesMatch = [regex]::Match($content, '(?ms)^rules:\s*\r?\n(.*)')
    if (-not $rulesMatch.Success) { throw "No 'rules:' section found in $Path" }
    $rulesText = $rulesMatch.Groups[1].Value

    # Split on rule entries "  - id: ..."
    $ruleBlocks = [regex]::Split($rulesText, '(?m)^\s{2}-\s+id:\s*') |
                  Where-Object { $_.Trim() -ne '' } |
                  Select-Object -Skip 0

    # First block starts with the id value; subsequent blocks are the remaining lines
    $ids = [regex]::Matches($rulesText, '(?m)^\s{2}-\s+id:\s*(.+)') | ForEach-Object { $_.Groups[1].Value.Trim().Trim('"') }
    $bodies = [regex]::Split($rulesText, '(?m)^\s{2}-\s+id:\s*[^\r\n]+\r?\n') | Select-Object -Skip 1

    for ($i = 0; $i -lt $ids.Count; $i++) {
        $id   = $ids[$i]
        $body = if ($i -lt $bodies.Count) { $bodies[$i] } else { '' }

        $rule = @{
            id              = $id
            className       = ''
            name            = ''
            description     = ''
            recommendation  = ''
            categoryCode    = ''
            scope           = ''
            type            = 'Rule'
            defaultSeverity = 'Error'
            defaultIsEnabled = $true
            requiredFeature = $null
            documentationLink = $null
            parameters      = @()
        }

        $strFields = @('className','name','description','recommendation','categoryCode',
                       'scope','type','defaultSeverity','documentationLink')
        foreach ($f in $strFields) {
            if ($body -match "(?m)^\s{4}$f\s*:\s*`"([^`"]*)`"") {
                $v = $Matches[1]
                $rule[$f] = if ($v -eq 'null' -or $v -eq '~') { $null } else { $v }
            } elseif ($body -match "(?m)^\s{4}$f\s*:\s*([^\r\n]+)") {
                $v = $Matches[1].Trim().Trim('"')
                $rule[$f] = if ($v -eq 'null' -or $v -eq '~') { $null } else { $v }
            }
        }

        if ($body -match '(?m)^\s{4}defaultIsEnabled\s*:\s*(\S+)') {
            $rule.defaultIsEnabled = $Matches[1].Trim() -eq 'true'
        }
        if ($body -match '(?m)^\s{4}requiredFeature\s*:\s*([^\r\n]+)') {
            $v = $Matches[1].Trim().Trim('"')
            $rule.requiredFeature = if ($v -eq 'null' -or $v -eq '~') { $null } else { $v }
        }

        # Parameters block (skip empty inline array "parameters: []")
        if ($body -match '(?ms)^\s{4}parameters\s*:\s*\r?\n((?:\s{6}[^\r\n]*\r?\n?)*)') {
            $paramsText  = $Matches[1]
            $paramBlocks = [regex]::Split($paramsText, '(?m)^\s{6}-\s+') |
                           Where-Object { $_.Trim() -ne '' }
            foreach ($pb in $paramBlocks) {
                $p = @{ key = ''; displayName = ''; defaultValue = '' }
                if ($pb -match '(?m)key\s*:\s*"?([^"\r\n]+)"?')          { $p.key          = $Matches[1].Trim().Trim('"') }
                if ($pb -match '(?m)displayName\s*:\s*"?([^"\r\n]+)"?')   { $p.displayName  = $Matches[1].Trim().Trim('"') }
                if ($pb -match '(?m)defaultValue\s*:\s*"?([^"\r\n]+)"?')  { $p.defaultValue = $Matches[1].Trim().Trim('"') }
                $rule.parameters += $p
            }
        }

        $result.rules += $rule
    }

    return $result
}

# ── Helpers ───────────────────────────────────────────────────────────────────

function EscapeCS([string] $s) { $s.Replace('\','\\').Replace('"','\"') }

function ScopeToAddRule([string] $scope) {
    switch ($scope) {
        'IActivityModel'  { return 'IActivityModel'  }
        'IWorkflowModel'  { return 'IWorkflowModel'  }
        'IProjectModel'   { return 'IProjectModel'   }
        'IProjectSummary' { return 'IProjectSummary' }
        default { throw "Unknown scope: $scope" }
    }
}

function ScopeToStubFactory([string] $scope) {
    switch ($scope) {
        'IActivityModel'  { return 'ActivityRule'  }
        'IWorkflowModel'  { return 'WorkflowRule'  }
        'IProjectModel'   { return 'ProjectRule'   }
        'IProjectSummary' { return 'SummaryRule'   }
        default { throw "Unknown scope: $scope" }
    }
}

# ── Phase 1: Load registry ────────────────────────────────────────────────────

Write-Host "Loading registry: $registryPath"
$registry = ConvertFrom-RegistryYaml -Path $registryPath
$regIndex = @{}
foreach ($r in $registry.rules) { $regIndex[$r.id] = $r }
Write-Host "  $($registry.rules.Count) rules indexed."

# ── Phase 2: Resolve manifest ─────────────────────────────────────────────────

Write-Host "Loading manifest: $manifestPath"
$manifest     = Get-Content $manifestPath -Raw | ConvertFrom-Json
$resolvedRules = @()

foreach ($entry in $manifest.rules) {
    $id = $entry.id
    if (-not $regIndex.ContainsKey($id)) {
        Write-Error "Rule '$id' declared in manifest but not found in registry."
        exit 1
    }
    $reg = $regIndex[$id]

    $resolved = @{
        id              = $id
        className       = $reg.className
        name            = $reg.name
        recommendation  = $reg.recommendation
        scope           = $reg.scope
        requiredFeature = $reg.requiredFeature
        parameters      = $reg.parameters
        severityOverride = $null
        paramOverrides  = @{}
    }

    if ($entry.PSObject.Properties['severity']) {
        $resolved.severityOverride = $entry.severity
    }

    if ($entry.PSObject.Properties['parameters']) {
        $entry.parameters.PSObject.Properties | ForEach-Object {
            $key = $_.Name
            $val = $_.Value
            # Validate key exists in registry
            $declared = $reg.parameters | Where-Object { $_.key -eq $key }
            if (-not $declared) {
                Write-Error "Parameter '$key' for rule '$id' not declared in registry."
                exit 1
            }
            $resolved.paramOverrides[$key] = $val
        }
    }

    $resolvedRules += $resolved
}

Write-Host "  $($resolvedRules.Count) rules resolved."

# ── Phase 3: Locate source files + read namespaces ────────────────────────────

Write-Host "Locating source files..."
$srcRoot = Join-Path $repoRoot "src\$($registry.package)"

foreach ($r in $resolvedRules) {
    $files = Get-ChildItem -Path $srcRoot -Recurse -Filter "$($r.className).cs" -ErrorAction SilentlyContinue
    if (-not $files) {
        Write-Error "Source file not found for class '$($r.className)' under $srcRoot"
        exit 1
    }
    $r.sourceFile = $files[0].FullName
    # Read namespace from first 20 lines
    $nsLine = Get-Content $r.sourceFile -TotalCount 20 | Select-String '^\s*namespace\s+(\S+)'
    if ($nsLine) {
        $r.namespace = $nsLine.Matches[0].Groups[1].Value.TrimEnd('{').Trim()
    } else {
        $r.namespace = $registry.namespace
    }
    $relPath = $r.sourceFile.Substring($repoRoot.Length).TrimStart('\')
    Write-Host "  $($r.id) -> $relPath  [$($r.namespace)]"
}

# Always include DotNetIdentifierValidator
$validatorFiles = Get-ChildItem -Path $srcRoot -Recurse -Filter 'DotNetIdentifierValidator.cs' -ErrorAction SilentlyContinue
$validatorRelPath = if ($validatorFiles) {
    $validatorFiles[0].FullName.Substring($repoRoot.Length).TrimStart('\')
} else { $null }

# Collect unique namespaces for using directives
$uniqueNamespaces = $resolvedRules | ForEach-Object { $_.namespace } | Select-Object -Unique | Sort-Object

# Partition rules
$noOverrideRules   = $resolvedRules | Where-Object { $_.severityOverride -eq $null -and $_.paramOverrides.Count -eq 0 }
$withOverrideRules = $resolvedRules | Where-Object { $_.severityOverride -ne $null -or  $_.paramOverrides.Count -gt 0 }

# Group override rules by requiredFeature
$overrideGroups = $withOverrideRules | Group-Object { if ($_.requiredFeature) { $_.requiredFeature } else { '__none__' } }

# ── Phase 4: Emit .csproj ─────────────────────────────────────────────────────

Write-Host "Emitting csproj: $csprojPath"
if (Test-Path $csprojPath) { Write-Warning "Overwriting existing $csprojPath" }

$sdkRelPath = '..\..\src\WatchfulAnvil.Sdk\WatchfulAnvil.Sdk.csproj'

$compileItems = ($resolvedRules | ForEach-Object {
    $rel = $_.sourceFile.Substring($repoRoot.Length).TrimStart('\')
    "    <Compile Include=`"..\..\$rel`" />"
}) -join "`n"

if ($validatorRelPath) {
    $compileItems = "    <Compile Include=`"..\..\$validatorRelPath`" />`n$compileItems"
}

$version     = if ($manifest.PSObject.Properties['version']) { $manifest.version } else { '1.0.0' }
$description = if ($manifest.PSObject.Properties['description']) { $manifest.description } else { '' }
$authors     = if ($manifest.PSObject.Properties['authors']) { $manifest.authors } else { '' }
$license     = if ($manifest.PSObject.Properties['license']) { $manifest.license } else { '' }

$registryRelPath = "..\..\registry\$orgPrefix\rules.yaml"

$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net461;net6.0;net8.0</TargetFrameworks>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <RootNamespace>Cpmf</RootNamespace>
    <AssemblyName>$PackageName</AssemblyName>
    <Version>$version</Version>
    <Authors>$authors</Authors>
    <PackageLicenseExpression>$license</PackageLicenseExpression>
    <PackageDescription>$description</PackageDescription>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <!-- net461: LangVersion 7.3, no nullable, no implicit usings -->
  <PropertyGroup Condition="'`$(TargetFramework)' == 'net461'">
    <LangVersion>7.3</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
  </PropertyGroup>

  <!-- net6.0 / net8.0: full SDK features -->
  <PropertyGroup Condition="'`$(TargetFramework)' != 'net461'">
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Rule source files: net6.0 and net8.0 only -->
  <ItemGroup Condition="'`$(TargetFramework)' != 'net461'">
$compileItems
  </ItemGroup>

  <!-- Generated bootstrapper: all targets -->
  <ItemGroup>
    <Compile Include="RegisterAnalyzerConfiguration.g.cs" />
  </ItemGroup>

  <!-- WatchfulAnvil.Sdk -->
  <ItemGroup>
    <ProjectReference Include="$sdkRelPath" />
  </ItemGroup>

  <!-- UiPath.Activities.Api -->
  <ItemGroup Condition="'`$(TargetFramework)' == 'net461' Or '`$(TargetFramework)' == 'net6.0'">
    <PackageReference Include="UiPath.Activities.Api" Version="23.10.3">
      <ExcludeAssets>runtime</ExcludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup Condition="'`$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="UiPath.Activities.Api" Version="24.10.1">
      <ExcludeAssets>runtime</ExcludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <!-- R5: registry catalog + manifest embedded in nupkg -->
  <ItemGroup>
    <Content Include="$registryRelPath" Pack="true" PackagePath="content\" />
    <Content Include="manifest.json" Pack="true" PackagePath="content\" />
  </ItemGroup>

</Project>
"@

Set-Content -Path $csprojPath -Value $csprojContent -Encoding UTF8

# ── Phase 5: Emit RegisterAnalyzerConfiguration.g.cs ─────────────────────────

Write-Host "Emitting bootstrapper: $bootstrapPath"
if (Test-Path $bootstrapPath) { Write-Warning "Overwriting existing $bootstrapPath" }

$needsRulesUsing = ($withOverrideRules | Measure-Object).Count -gt 0
$needsSeverity   = ($withOverrideRules | Where-Object { $_.severityOverride -ne $null } | Measure-Object).Count -gt 0

function Emit-OverrideRule {
    param([System.Text.StringBuilder] $Builder, [hashtable] $Rule, [string] $Indent = '')

    $pfx   = "            $Indent"
    $model = ScopeToAddRule $Rule.scope

    $null = $Builder.AppendLine("$pfx{")
    $null = $Builder.AppendLine("$pfx    var rule = new $($Rule.className)().Get();")

    if ($Rule.severityOverride) {
        $level = switch ($Rule.severityOverride) {
            'Error'   { 'TraceLevel.Error'   }
            'Warning' { 'TraceLevel.Warning' }
            'Info'    { 'TraceLevel.Info'    }
            default   { "TraceLevel.$($Rule.severityOverride)" }
        }
        $null = $Builder.AppendLine("$pfx    rule.DefaultErrorLevel = $level;")
    }

    foreach ($key in $Rule.paramOverrides.Keys) {
        $val = EscapeCS $Rule.paramOverrides[$key]
        $null = $Builder.AppendLine("$pfx    rule.Parameters[`"$key`"].Value = `"$val`";")
        $null = $Builder.AppendLine("$pfx    rule.Parameters[`"$key`"].DefaultValue = `"$val`";")
    }

    $null = $Builder.AppendLine("$pfx    api.AddRule<$model>(rule);")
    $null = $Builder.AppendLine("$pfx}")
}

$sb2 = [System.Text.StringBuilder]::new()

$null = $sb2.AppendLine('// <auto-generated> tools/New-DeliveryPackage.ps1 -- do not edit manually </auto-generated>')
$null = $sb2.AppendLine('using WatchfulAnvil.Sdk.Core;')
$null = $sb2.AppendLine('using UiPath.Studio.Activities.Api;')
$null = $sb2.AppendLine('using UiPath.Studio.Activities.Api.Analyzer;')

if ($needsRulesUsing) {
    $null = $sb2.AppendLine('using UiPath.Studio.Activities.Api.Analyzer.Rules;')
}
if ($needsSeverity) {
    $null = $sb2.AppendLine('using System.Diagnostics;')
}

$null = $sb2.AppendLine('#if !NET461')
if ($needsRulesUsing) {
    # AddRule<T> calls need the model types in scope
    $null = $sb2.AppendLine('using UiPath.Studio.Analyzer.Models;')
}
foreach ($ns in $uniqueNamespaces) {
    $null = $sb2.AppendLine("using $ns;")
}
$null = $sb2.AppendLine('#endif')
$null = $sb2.AppendLine('')
$null = $sb2.AppendLine('namespace Cpmf')
$null = $sb2.AppendLine('{')
$null = $sb2.AppendLine('    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration')
$null = $sb2.AppendLine('    {')
$null = $sb2.AppendLine('        public void Initialize(IAnalyzerConfigurationService api)')
$null = $sb2.AppendLine('        {')
$null = $sb2.AppendLine('#if NET461')
$null = $sb2.AppendLine('            // Stub registrations -- rules visible in Studio but always pass.')
foreach ($r in $resolvedRules) {
    $factory = ScopeToStubFactory $r.scope
    $name    = EscapeCS $r.name
    $rec     = EscapeCS $r.recommendation
    $null = $sb2.AppendLine("            StubRule.$factory(`"$($r.id)`", `"$name`", `"$rec`").Initialize(api);")
}
$null = $sb2.AppendLine('#else')

if ($noOverrideRules) {
    $null = $sb2.AppendLine('            // No overrides -- delegate to rule own Initialize()')
    foreach ($r in $noOverrideRules) {
        $null = $sb2.AppendLine("            new $($r.className)().Initialize(api);")
    }
}

foreach ($group in $overrideGroups) {
    $feature = $group.Name
    $rules   = $group.Group

    if ($feature -eq '__none__') {
        $null = $sb2.AppendLine('            // Overrides -- no feature gate required')
        foreach ($r in $rules) { Emit-OverrideRule $sb2 $r }
    } else {
        $null = $sb2.AppendLine("            // Overrides -- requires $feature")
        $null = $sb2.AppendLine("            if (api.HasFeature(DesignFeatureKeys.$feature))")
        $null = $sb2.AppendLine('            {')
        foreach ($r in $rules) { Emit-OverrideRule $sb2 $r -Indent '    ' }
        $null = $sb2.AppendLine('            }')
    }
}

$null = $sb2.AppendLine('#endif')
$null = $sb2.AppendLine('        }')
$null = $sb2.AppendLine('    }')
$null = $sb2.AppendLine('}')

Set-Content -Path $bootstrapPath -Value $sb2.ToString() -Encoding UTF8

Write-Host ""
Write-Host "Done. Generated files:"
Write-Host "  $csprojPath"
Write-Host "  $bootstrapPath"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  dotnet sln add `"$($csprojPath.Substring($repoRoot.Length + 1))`""
Write-Host "  dotnet build `"$($csprojPath.Substring($repoRoot.Length + 1))`" -c Release"
