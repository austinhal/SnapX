#!/usr/bin/env bash
set -euo pipefail

# Usage: bash scripts/create-dmg.sh [version] [app-path]
# Produces dist/SnapX-<version>.dmg with a /Applications symlink

VERSION="${1:-1.0.0}"
APP_DIR="${2:-dist/SnapX.app}"
APP_NAME="SnapX"
DMG_NAME="${APP_NAME}-${VERSION}.dmg"
STAGING="dist/dmg-staging"
DIST="$(dirname "$APP_DIR")"

echo "Creating $DMG_NAME..."

# Create staging directory with app + Applications symlink
rm -rf "$STAGING"
mkdir -p "$STAGING"
cp -r "$APP_DIR" "$STAGING/$APP_NAME.app"
ln -s /Applications "$STAGING/Applications"

# Determine size (add 10 MB headroom)
SIZE_KB=$(du -sk "$STAGING" | awk '{print $1}')
SIZE_MB=$(( (SIZE_KB / 1024) + 10 ))

# Create writable DMG
hdiutil create \
    -volname "$APP_NAME" \
    -srcfolder "$STAGING" \
    -ov \
    -format UDRW \
    -size "${SIZE_MB}m" \
    "$DIST/rw-${DMG_NAME}"

# Convert to compressed read-only
hdiutil convert "$DIST/rw-${DMG_NAME}" \
    -format UDZO \
    -imagekey zlib-level=9 \
    -o "$DIST/$DMG_NAME"

rm -f "$DIST/rw-${DMG_NAME}"
rm -rf "$STAGING"

echo "Created: $DIST/$DMG_NAME"
echo "Size: $(du -sh "$DIST/$DMG_NAME" | awk '{print $1}')"
