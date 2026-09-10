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

# Pack Cpmf rules AND the SDK they depend on into nupkg/.
# Both are needed: packs are no longer self-contained, so a feed holding only the
# rule pack leaves uipcli unable to restore WatchfulAnvil.Sdk. WatchfulAnvilPackDev
# opts this project into packing; it is deliberately not published by CI.
pack:
    dotnet pack src/WatchfulAnvil.Sdk/WatchfulAnvil.Sdk.csproj -c Release -o nupkg/
    dotnet pack src/Cpmf.WorkflowAnalyzerRules/Cpmf.WorkflowAnalyzerRules.csproj \
        -c Release -o nupkg/ -p:WatchfulAnvilPackDev=true

# Pack the Library rules package and copy to local NuGet feed
pack-libs:
    dotnet pack src/WatchfulAnvil.Sdk/WatchfulAnvil.Sdk.csproj -c Release -o nupkg/
    dotnet pack src/Cpmf.Rules.Libs/Cpmf.Rules.Libs.csproj \
        -c Release -o nupkg/
    cp nupkg/Cpmf.Rules.Libs.*.nupkg nupkg/WatchfulAnvil.Sdk.*.nupkg "C:/Users/Public/Documents/myNugetPackages/"

# Pack every release artefact (the curated dist/* packs) plus the SDK
pack-dist:
    dotnet pack src/WatchfulAnvil.Sdk/WatchfulAnvil.Sdk.csproj -c Release -o nupkg/
    dotnet pack dist/Cpmf.Standard/Cpmf.Standard.csproj -c Release -o nupkg/
    dotnet pack dist/Cpmf.Tap/Cpmf.Tap.csproj -c Release -o nupkg/
    dotnet pack dist/Cpmf.Community/Cpmf.Community.csproj -c Release -o nupkg/
    dotnet pack dist/Cpmf.Community.Preview/Cpmf.Community.Preview.csproj -c Release -o nupkg/
    dotnet pack dist/Mc.2026-04/Mc.2026-04.csproj -c Release -o nupkg/

# Bump patch version, pack, and copy to local NuGet feed
bump-patch:
    pwsh scripts/bump-version.ps1 -Part Patch

bump-minor:
    pwsh scripts/bump-version.ps1 -Part Minor

# ── Tools ─────────────────────────────────────────────────────────────────────

# Check EVERY rule registry against C# source (drift detection).
# Without --all only registry/Cpmf was validated, leaving Cpmf.Rules.Libs, CpmfTap and
# Mc unchecked - and `ci` inherited that gap.
check-rules:
    uv run tools/rule-inventory/run.py --check --all

# Print rule inventory as markdown
rules-md:
    uv run tools/rule-inventory/run.py --format markdown

# Report available versions from the remote feeds (what we pin vs what exists)
feed-versions:
    uv run tools/feed-versions/run.py

# List every published version of one package
feed-versions-all PACKAGE:
    uv run tools/feed-versions/run.py --package "{{PACKAGE}}" --all

# Fail if any pinned dependency has a newer release
feed-versions-check:
    uv run tools/feed-versions/run.py --check

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

# Assert every produced package ships the WatchfulAnvil dependencies it declares.
# Packs are no longer self-contained, so a pack published without its SDK gives
# consumers an unresolvable dependency - silently, at restore time.
check-closure:
    pwsh scripts/Test-PackageClosure.ps1

# Pack the release artefacts and assert closure over them
pack-check: pack-dist check-closure

# ── CI-equivalent (build + test + check) ─────────────────────────────────────

ci: build-all test check-rules
