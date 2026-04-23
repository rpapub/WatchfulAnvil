# WatchfulAnvil — development task runner
# Install: https://github.com/casey/just
# Usage:   just <recipe>

# Default: list available recipes
default:
    @just --list

# ── Build ─────────────────────────────────────────────────────────────────────

# Build the SDK and main rules project
build:
    dotnet build src/WatchfulAnvil.Sdk/WatchfulAnvil.Sdk.csproj
    dotnet build src/Cpmf.WorkflowAnalyzerRules/Cpmf.WorkflowAnalyzerRules.csproj

# Build every project under src/
build-all:
    dotnet build src/

# ── Test ──────────────────────────────────────────────────────────────────────

# Run all unit tests
test:
    dotnet test tests/

# Run only the Cpmf rules tests (faster)
test-cpmf:
    dotnet test tests/Cpmf.WorkflowAnalyzerRules.Tests/

# Run all tests with coverage
test-coverage:
    dotnet test tests/ --collect:"XPlat Code Coverage" --results-directory ./TestResults

# ── Pack ──────────────────────────────────────────────────────────────────────

# Pack Cpmf rules into nupkg/ (uses current csproj version)
pack:
    dotnet pack src/Cpmf.WorkflowAnalyzerRules/Cpmf.WorkflowAnalyzerRules.csproj \
        -c Release -o nupkg/

# Pack the TAP rules package
pack-tap:
    dotnet pack src/Cpmf.WorkflowAnalyzerRules/Cpmf.WorkflowAnalyzerRules.csproj \
        -c Release -o nupkg/ -p:PackId=Cpmf.Tap

# Bump patch version, pack, and copy to local NuGet feed
bump-patch:
    pwsh scripts/bump-version.ps1 -Part Patch

bump-minor:
    pwsh scripts/bump-version.ps1 -Part Minor

# ── Tools ─────────────────────────────────────────────────────────────────────

# Check rule registry against C# source (drift detection)
check-rules:
    uv run tools/rule-inventory/run.py --check

# Print rule inventory as markdown
rules-md:
    uv run tools/rule-inventory/run.py --format markdown

# Run rules against a project (set PROJECT=path/to/project.json)
analyze PROJECT="":
    uv run tools/analyze/run.py --project "{{PROJECT}}"

# Run TAP inspection against a project
tap PROJECT="":
    uv run tools/analyze/tap.py --project "{{PROJECT}}"

# Run corpus harness against default corpus location
corpus:
    uv run tools/corpus-harness/run.py

# Run corpus harness with custom corpus path
corpus-at PATH:
    uv run tools/corpus-harness/run.py --corpus "{{PATH}}"

# ── CI-equivalent (build + test + check) ─────────────────────────────────────

ci: build-all test check-rules
