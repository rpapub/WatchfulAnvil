#!/bin/bash

# ------------------------------------------------------------------------------
# normalize-images-1600x900.sh
# Normalize all images in selected folders to 1600x900, resizing or padding
# Background: solarized base3 (#fdf6e3)
# ------------------------------------------------------------------------------
# Usage:
#   ./tools/normalize-images-1600x900.sh
# Run from repo root.
# ------------------------------------------------------------------------------

set -euo pipefail

TARGET_WIDTH=1600
TARGET_HEIGHT=900
BACKGROUND="#fdf6e3"

# Must be run from project root
ROOT_DIR="$(pwd)"

# Directories to process (relative to root)
DIRS=(
  "docs/assets/img/getting-started"
  "docs/assets/img/incoming"
  "docs/assets/img/logo"
)

# Check dependencies
if ! command -v convert >/dev/null || ! command -v identify >/dev/null; then
  echo "❌ ImageMagick (convert + identify) is required." >&2
  exit 1
fi

echo "📐 Normalizing images to ${TARGET_WIDTH}x${TARGET_HEIGHT}"
echo "🎨 Background: $BACKGROUND"

# Loop through each directory
for DIR in "${DIRS[@]}"; do
  ABS_DIR="$ROOT_DIR/$DIR"
  OUT_DIR="$ABS_DIR/normalized"

  echo "📁 Processing: $DIR"
  rm -rf "$OUT_DIR"
  mkdir -p "$OUT_DIR"

    shopt -s nullglob
    echo "🔎 Looking for: $ABS_DIR/*.png"
    for IMG in "$ABS_DIR"/*.png; do
    echo "→ Found: $IMG"

    BASENAME=$(basename "$IMG")
    OUT="$OUT_DIR/$BASENAME"

IDENTIFY_OUT=$(identify -format "%w %h" "$IMG" 2>/dev/null || true)

if [[ -z "$IDENTIFY_OUT" ]]; then
  echo "   ⚠️  Skipping unreadable image: $IMG"
  continue
fi

read WIDTH HEIGHT <<< "$IDENTIFY_OUT"

    echo "   Dimensions: ${WIDTH}x${HEIGHT}"

    if [[ "$WIDTH" -gt $TARGET_WIDTH || "$HEIGHT" -gt $TARGET_HEIGHT ]]; then
        echo "   Action: resize + pad"
        convert "$IMG" \
        -resize ${TARGET_WIDTH}x${TARGET_HEIGHT}\> \
        -gravity center \
        -background "$BACKGROUND" \
        -extent ${TARGET_WIDTH}x${TARGET_HEIGHT} \
        "$OUT"
    elif [[ "$WIDTH" -lt $TARGET_WIDTH || "$HEIGHT" -lt $TARGET_HEIGHT ]]; then
        echo "   Action: pad only"
        convert "$IMG" \
        -gravity center \
        -background "$BACKGROUND" \
        -extent ${TARGET_WIDTH}x${TARGET_HEIGHT} \
        "$OUT"
    else
        echo "   Action: copy (already 1600x900)"
        cp "$IMG" "$OUT"
    fi

    echo "✓ $BASENAME → $OUT_DIR"
    done

done

echo "✅ Done."
