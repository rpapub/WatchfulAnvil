#!/usr/bin/env bash
# Probe whether the rule packs build and pack on Linux.
# Each target is attempted independently; a failure is recorded, not fatal,
# so one run reports the status of every target.

set -u
shopt -s nullglob

LOG=/out/build.log
mkdir -p /out
: > "$LOG"

log() { echo "$@" | tee -a "$LOG"; }

log "=== environment ==="
log "dotnet $(dotnet --version)"
log "$(uname -srm)"
log ""

# Copy the read-only mount into a writable tree, excluding build leftovers and
# the Windows-side artefact dirs so nothing stale is picked up.
log "=== staging repo ==="
mkdir -p /work
tar -C /repo \
    --exclude='./.git' \
    --exclude='./.vs' \
    --exclude='*/bin' --exclude='*/obj' \
    --exclude='./nupkg' --exclude='./artifacts' --exclude='./dist/*/bin' \
    --exclude='./infra/linux-build/out' \
    -cf - . 2>/dev/null | tar -C /work -xf -
log "staged $(find /work -name '*.csproj' | wc -l) csproj files"
log ""

# target label | project path | extra msbuild args
TARGETS=(
  "Cpmf.WorkflowAnalyzerRules (net6.0;net8.0, ILRepack)|src/Cpmf.WorkflowAnalyzerRules/Cpmf.WorkflowAnalyzerRules.csproj|"
  "Cpmf.Rules.Libs (net6.0;net8.0, ILRepack)|src/Cpmf.Rules.Libs/Cpmf.Rules.Libs.csproj|"
  "WatchfulAnvil.Sdk (net461;net6.0;net8.0)|src/WatchfulAnvil.Sdk/WatchfulAnvil.Sdk.csproj|-p:IsPackable=true"
)

declare -a RESULTS=()

for entry in "${TARGETS[@]}"; do
  IFS='|' read -r label proj extra <<< "$entry"
  log "==================================================================="
  log "=== $label"
  log "=== $proj"
  log "==================================================================="

  # shellcheck disable=SC2086
  if dotnet pack "/work/$proj" \
        -c Release \
        --configfile /nuget/NuGet.config \
        -o /out/nupkg \
        $extra >>"$LOG" 2>&1; then
    RESULTS+=("PASS|$label")
    log ">>> PASS"
  else
    RESULTS+=("FAIL|$label")
    log ">>> FAIL (see log above)"
  fi
  log ""
done

log "==================================================================="
log "=== SUMMARY"
log "==================================================================="
for r in "${RESULTS[@]}"; do
  IFS='|' read -r status label <<< "$r"
  log "$(printf '%-4s' "$status")  $label"
done
log ""

log "=== produced packages ==="
for f in /out/nupkg/*.nupkg; do
  log "$(printf '%8d' "$(stat -c%s "$f")")  $(basename "$f")"
done
log ""

# The decisive check: did ILRepack actually merge, or silently no-op?
log "=== ILRepack verification (merged assemblies contain SDK types) ==="
for f in /out/nupkg/Cpmf.WorkflowAnalyzerRules.*.nupkg /out/nupkg/Cpmf.Rules.Libs.*.nupkg; do
  log "--- $(basename "$f")"
  unzip -l "$f" 2>/dev/null | awk '/lib\/.*\.dll/ {printf "    %8s  %s\n", $1, $4}' | tee -a "$LOG" >/dev/null
  unzip -l "$f" 2>/dev/null | awk '/lib\/.*\.dll/ {printf "    %8s  %s\n", $1, $4}'
done

log ""
log "A merged pack DLL should be ~70-80 KB (SDK is ~72 KB)."
log "A pack DLL under ~15 KB means ILRepack did not merge."
