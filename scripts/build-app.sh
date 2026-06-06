#!/usr/bin/env bash
set -euo pipefail

# Usage: bash scripts/build-app.sh [version]
# Produces dist/SnapX.app

VERSION="${1:-1.0.0}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$SCRIPT_DIR/.."
APP_NAME="SnapX"
DIST="$ROOT/dist"
APP_DIR="$DIST/$APP_NAME.app"
CONTENTS="$APP_DIR/Contents"
MACOS="$CONTENTS/MacOS"
RESOURCES="$CONTENTS/Resources"

echo "Building $APP_NAME.app v$VERSION..."

# Clean dist
rm -rf "$DIST"
mkdir -p "$MACOS" "$RESOURCES"

# Publish arm64 self-contained
dotnet publish "$ROOT/ShareXMac/ShareXMac.csproj" \
    --runtime osx-arm64 \
    --self-contained true \
    --configuration Release \
    --output "$MACOS" \
    /p:DebugType=None \
    /p:DebugSymbols=false

# Verify executable exists
if [ ! -f "$MACOS/SnapX" ]; then
    echo "ERROR: SnapX executable not found in $MACOS" >&2
    exit 1
fi

# Copy and patch Info.plist
cp "$ROOT/ShareXMac/Info.plist" "$CONTENTS/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $VERSION" "$CONTENTS/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $VERSION" "$CONTENTS/Info.plist"

# Copy icon
cp "$ROOT/ShareXMac/Assets/AppIcon.icns" "$RESOURCES/AppIcon.icns"

echo ""
echo "Built: $APP_DIR"
echo "Structure:"
echo "  Contents/Info.plist"
echo "  Contents/MacOS/SnapX  (+ $(ls "$MACOS" | wc -l | xargs) other files)"
echo "  Contents/Resources/AppIcon.icns"
