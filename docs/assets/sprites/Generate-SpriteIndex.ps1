<#
.SYNOPSIS
Generates index.html and index.json with structured sprite metadata.

.DESCRIPTION
- index.html: browsable file list
- index.json: detailed file data + machine-readable metadata
- Groups by 'bar-' and 'circle-' prefixes
- Extracts color, shape, width, height from filenames

Author: Christian Prior-Mamulyan
License: CC-BY
#>

$outputHtml = "index.html"
$outputJson = "index.json"

# Get all .svg files
$svgFiles = Get-ChildItem -File -Filter *.svg | Sort-Object Name

# Prepare JSON object
$json = [ordered]@{
    metadata = [ordered]@{
        colors = @()
        shapes = @()
        sizes  = @()
    }
    files = @()
}

# -----------------------
# Process Files
# -----------------------

foreach ($file in $svgFiles) {
    if ($file.Name -match '^(bar|circle)-([a-z0-9]+)-(\d+)x(\d+)\.svg$') {
        $shape  = $matches[1]
        $color  = $matches[2]
        $width  = [int]$matches[3]
        $height = [int]$matches[4]

        # Collect metadata
        if ($json.metadata.colors -notcontains $color) {
            $json.metadata.colors += $color
        }
        if ($json.metadata.shapes -notcontains $shape) {
            $json.metadata.shapes += $shape
        }

        $size = @{ width = $width; height = $height }
        $existingSize = $json.metadata.sizes | Where-Object {
            $_.width -eq $width -and $_.height -eq $height
        }
        if (-not $existingSize) {
            $json.metadata.sizes += $size
        }

        # Add to files
        $json.files += [ordered]@{
            filename = $file.Name
            shape    = $shape
            color    = $color
            width    = $width
            height   = $height
        }
    }
}

# -----------------------
# Write JSON
# -----------------------

$json | ConvertTo-Json -Depth 3 | Set-Content -Path $outputJson -Encoding UTF8

# -----------------------
# HTML index generation
# -----------------------

$bars = $svgFiles | Where-Object { $_.Name -like "bar-*" }
$circles = $svgFiles | Where-Object { $_.Name -like "circle-*" }

$html = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <title>Solarized SVG Sprite Index</title>
</head>
<body>
  <h1>Solarized SVG Sprite Index</h1>
  <p>Right-click any link below and choose “Copy link address”.</p>

  <h2>Bars</h2>
  <ul>
"@

foreach ($file in $bars) {
    $html += "    <li><a href='$($file.Name)'>$($file.Name)</a></li>`n"
}

$html += @"
  </ul>

  <h2>Circles</h2>
  <ul>
"@

foreach ($file in $circles) {
    $html += "    <li><a href='$($file.Name)'>$($file.Name)</a></li>`n"
}

$html += @"
  </ul>
</body>
</html>
"@

$html | Set-Content -Path $outputHtml -Encoding UTF8

Write-Host "Generated $outputHtml and $outputJson with $($json.files.Count) entries."
