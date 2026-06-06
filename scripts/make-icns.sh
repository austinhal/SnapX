#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$SCRIPT_DIR/.."
SRC_ICO="$ROOT/ShareXMac/Assets/icon.ico"
ICONSET_DIR="$ROOT/ShareXMac/Assets/AppIcon.iconset"
OUT_ICNS="$ROOT/ShareXMac/Assets/AppIcon.icns"
TMP_PNG=$(mktemp).png

echo "Generating AppIcon.icns from icon.ico..."

# Extract PNG from ICO using Python (PIL)
python3 << EOF
from PIL import Image
ico = Image.open("$SRC_ICO")
ico.save("$TMP_PNG")
EOF

rm -rf "$ICONSET_DIR"
mkdir -p "$ICONSET_DIR"

# Use the extracted PNG as source; largest source icon is 256x256
# Only use sizes <= 256x256 to avoid upscaling artifacts
sips -z 16   16   "$TMP_PNG" --out "$ICONSET_DIR/icon_16x16.png"      >/dev/null
sips -z 32   32   "$TMP_PNG" --out "$ICONSET_DIR/icon_16x16@2x.png"   >/dev/null
sips -z 32   32   "$TMP_PNG" --out "$ICONSET_DIR/icon_32x32.png"       >/dev/null
sips -z 64   64   "$TMP_PNG" --out "$ICONSET_DIR/icon_32x32@2x.png"   >/dev/null
sips -z 128  128  "$TMP_PNG" --out "$ICONSET_DIR/icon_128x128.png"     >/dev/null
sips -z 256  256  "$TMP_PNG" --out "$ICONSET_DIR/icon_128x128@2x.png" >/dev/null
sips -z 256  256  "$TMP_PNG" --out "$ICONSET_DIR/icon_256x256.png"     >/dev/null
sips -z 256  256  "$TMP_PNG" --out "$ICONSET_DIR/icon_256x256@2x.png" >/dev/null

iconutil -c icns "$ICONSET_DIR" -o "$OUT_ICNS"
rm -rf "$ICONSET_DIR" "$TMP_PNG"

echo "Created: $OUT_ICNS"
