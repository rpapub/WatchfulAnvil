<#
.SYNOPSIS
Generates SVG sprite files for all 16 Solarized colors.

.DESCRIPTION
Creates rectangular bar and circular icon SVG files in various dimensions, suitable for use in documentation styling, UI mockups, or visual metadata indicators. Output includes:

- Bars: 720x16, 360x8, 8x8, 16x16
- Circles: 4x4, 8x8, 16x16

Each file is written to the same directory as the script.

.LICENSE
Creative Commons Attribution (CC BY)

.AUTHOR
Christian Prior-Mamulyan
cprior@gmail.com
#>

# Define Solarized color palette
$colors = @{
    base03 = "#002b36"
    base02 = "#073642"
    base01 = "#586e75"
    base00 = "#657b83"
    base0  = "#839496"
    base1  = "#93a1a1"
    base2  = "#eee8d5"
    base3  = "#fdf6e3"
    yellow = "#b58900"
    orange = "#cb4b16"
    red    = "#dc322f"
    magenta= "#d33682"
    violet = "#6c71c4"
    blue   = "#268bd2"
    cyan   = "#2aa198"
    green  = "#859900"
}

# Bar sprite sizes (width x height)
$barSizes = @(
    @{ name = "720x16"; width = 720; height = 16 },
    @{ name = "360x8";  width = 360; height = 8 },
    @{ name = "8x8";    width = 8;   height = 8 },
    @{ name = "16x16";  width = 16;  height = 16 }
)

# Circle sprite sizes (square canvas)
$circleSizes = @(4, 8, 16)

# Output directory = script's location
$outputDir = $PSScriptRoot

# ------------------------
# Generate bar rectangle SVGs
# ------------------------
foreach ($colorName in $colors.Keys) {
    $colorHex = $colors[$colorName]

    foreach ($size in $barSizes) {
        $fileName = "bar-$colorName-$($size.name).svg"
        $filePath = Join-Path $outputDir $fileName

        # Create SVG markup for a solid-color rectangle
        $svg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="$($size.width)" height="$($size.height)">
  <rect width="$($size.width)" height="$($size.height)" fill="$colorHex"/>
</svg>
"@

        # Save the SVG to file
        Set-Content -Path $filePath -Value $svg -Encoding UTF8
    }
}

# ------------------------
# Generate circle SVGs
# ------------------------
foreach ($colorName in $colors.Keys) {
    $colorHex = $colors[$colorName]

    foreach ($dim in $circleSizes) {
        $radius = [math]::Floor($dim / 2)
        $cx = $radius
        $cy = $radius
        $fileName = "circle-$colorName-${dim}x$dim.svg"
        $filePath = Join-Path $outputDir $fileName

        # Create SVG markup for a centered filled circle
        $svg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="$dim" height="$dim">
  <circle cx="$cx" cy="$cy" r="$radius" fill="$colorHex" />
</svg>
"@

        # Save the SVG to file
        Set-Content -Path $filePath -Value $svg -Encoding UTF8
    }
}
