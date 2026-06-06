#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$SCRIPT_DIR/.."
SRC_ICO="$ROOT/ShareXMac/Assets/icon.ico"
ICONSET_DIR="$ROOT/ShareXMac/Assets/AppIcon.iconset"
OUT_ICNS="$ROOT/ShareXMac/Assets/AppIcon.icns"
TMP_PNG=$(mktemp).png

echo "Generating AppIcon.icns from icon.ico..."

python3 -c "from PIL import Image" 2>/dev/null || {
    echo "Error: PIL (Pillow) Python package is required"
    echo "Install with: pip install Pillow"
    exit 1
}

# sips cannot properly convert ICO to PNG format on macOS, so we use minimal Python
# to extract the largest image from the ICO file as PNG, then process with sips
python3 << EOF
from PIL import Image
ico = Image.open("$SRC_ICO")
ico.save("$TMP_PNG")
EOF

rm -rf "$ICONSET_DIR"
mkdir -p "$ICONSET_DIR"

# sips reads PNG and outputs properly formatted PNGs
# Largest source icon is 256x256; scale up for @2x sizes
sips -z 16   16   "$TMP_PNG" --out "$ICONSET_DIR/icon_16x16.png"      >/dev/null
sips -z 32   32   "$TMP_PNG" --out "$ICONSET_DIR/icon_16x16@2x.png"   >/dev/null
sips -z 32   32   "$TMP_PNG" --out "$ICONSET_DIR/icon_32x32.png"       >/dev/null
sips -z 64   64   "$TMP_PNG" --out "$ICONSET_DIR/icon_32x32@2x.png"   >/dev/null
sips -z 128  128  "$TMP_PNG" --out "$ICONSET_DIR/icon_128x128.png"     >/dev/null
sips -z 256  256  "$TMP_PNG" --out "$ICONSET_DIR/icon_128x128@2x.png" >/dev/null
sips -z 256  256  "$TMP_PNG" --out "$ICONSET_DIR/icon_256x256.png"     >/dev/null
sips -z 512  512  "$TMP_PNG" --out "$ICONSET_DIR/icon_256x256@2x.png" >/dev/null
sips -z 512  512  "$TMP_PNG" --out "$ICONSET_DIR/icon_512x512.png"     >/dev/null
sips -z 1024 1024 "$TMP_PNG" --out "$ICONSET_DIR/icon_512x512@2x.png" >/dev/null

iconutil -c icns -o "$OUT_ICNS" "$ICONSET_DIR"
rm -rf "$ICONSET_DIR" "$TMP_PNG"

echo "Created: $OUT_ICNS"
