#!/bin/bash

# ------------------------------------------------------------------------------
# build-animations.sh
# Assemble animated GIFs (1600x900) from normalized images
# Appends a final solarized base03 (#002b36) frame
# ------------------------------------------------------------------------------
# Requires: images normalized in subfolders, ImageMagick
# Run from project root
# ------------------------------------------------------------------------------

set -euo pipefail

OUTDIR="./animations"
NORMALIZED_DIR="docs/assets/img/incoming/normalized"
DARK_FRAME="./frames/frame__base03__1600x900.png"

# Define delays at the top of the script
DEFAULT_DELAY=250    # default delay for regular frames
EXTENDED_DELAY=400  # extended delay for the last frame (base03 frame)

mkdir -p "$OUTDIR" "./frames"

# Create base03 frame if missing
if [[ ! -f "$DARK_FRAME" ]]; then
  convert -size 1600x900 canvas:"#002b36" "$DARK_FRAME"
  echo "✓ Created dark frame: $DARK_FRAME"
fi

# Source the image paths from external config
source "./tools/image_paths.conf"

# --- Function to build GIF from normalized files ---
build_gif () {
  local -n image_list=$1
  local base_filename=$2
  local normalized=()

  local count_raw=${#image_list[@]}
  local count=$(printf "%03d" "$count_raw")
  local output_filename="${base_filename//__COUNT__/$count}"

  echo "🔧 Building $output_filename (${count_raw} frames)..."

  for img in "${image_list[@]}"; do
    local path="$NORMALIZED_DIR/$(basename "$img")"
    if [[ ! -f "$path" ]]; then
      echo "⚠️  Skipping missing file: $path"
      continue
    fi
    normalized+=("$path")
  done

  # Append final dark frame
  normalized+=("$DARK_FRAME")

  convert \
    $(printf -- "-delay $DEFAULT_DELAY %s " "${normalized[@]::${#normalized[@]}-1}") \
    -delay $EXTENDED_DELAY "${normalized[-1]}" \
    -loop 0 "$OUTDIR/$output_filename"

  echo "🎞️  Created: $OUTDIR/$output_filename"
}

# --- Build all animations ---
build_gif REPO_CLONE getting_started__repo-clone____COUNT____animation__en__1600x900.gif
build_gif NUGET_FEED getting_started__customize-nuget-feeds____COUNT____animation__en__1600x900.gif
build_gif BUILD_HELLO_WORLD_RULE getting_started__build-hello-world-rule____COUNT____animation__en__1600x900.gif

exit 0;
#build_gif DOWNLOAD_IMAGES getting_started__download____COUNT____animation__en__1600x900.gif
#build_gif NET461_IMAGES getting_started__net461____COUNT____animation__en__1600x900.gif
#build_gif SDK60_IMAGES getting_started__sdk60____COUNT____animation__en__1600x900.gif
#build_gif SDK80_IMAGES getting_started__sdk80____COUNT____animation__en__1600x900.gif
#build_gif VISUALSTUDIO_IMAGES getting_started__visualstudio____COUNT____animation__en__1600x900.gif


