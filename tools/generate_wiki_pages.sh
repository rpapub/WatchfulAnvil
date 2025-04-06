#!/bin/bash

# ------------------------------------------------------------------------------
# generate_wiki_pages.sh
# Generates GitHub wiki pages with a sequence of images based on the arrays in image_paths.conf
# ------------------------------------------------------------------------------
# Run from project root
# ------------------------------------------------------------------------------

set -euo pipefail

# Source the image paths from the external config file
source "./tools/image_paths.conf"

# Define output folder (can be customized)
OUTPUT_DIR="./generated_wiki_pages"

# Create output directory if it doesn't exist
mkdir -p "$OUTPUT_DIR"

# Base URL for the hosted images
BASE_URL="https://rpapub.github.io/WatchfulAnvil/assets/img/getting-started"

# Function to generate the markdown for a sequence of images
generate_page () {
  local -n image_list=$1
  local output_filename=$2

  echo "🔧 Generating wiki page: $output_filename..."

  # Start the markdown file content
  echo "# Image Sequence for $output_filename" > "$OUTPUT_DIR/$output_filename.md"
  echo "" >> "$OUTPUT_DIR/$output_filename.md"

  # Loop through the images and add img tags to markdown
  for img in "${image_list[@]}"; do
    local image_name=$(basename "$img")
    echo "<img src=\"$BASE_URL/$image_name\" alt=\"$image_name\" style=\"width:100%; height:auto;\" />" >> "$OUTPUT_DIR/$output_filename.md"
    echo "" >> "$OUTPUT_DIR/$output_filename.md"  # add a newline for separation
  done

  echo "🎉 Generated: $OUTPUT_DIR/$output_filename.md"
}

# Generate pages for each image array
generate_page NET461_IMAGES "net461_images"
generate_page SDK60_IMAGES "sdk60_images"
generate_page SDK80_IMAGES "sdk80_images"
generate_page VISUALSTUDIO_IMAGES "visualstudio_images"
generate_page REPO_CLONE "repo_clone"
generate_page NUGET_FEED "nuget_feed"
generate_page BUILD_HELLO_WORLD_RULE "build_hello_world_rule"

echo "✅ All wiki pages generated successfully!"
