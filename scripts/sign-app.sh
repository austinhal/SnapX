#!/usr/bin/env bash
set -euo pipefail

# Usage: bash scripts/sign-app.sh [app-path]
# Signs with DEVELOPER_ID env var if set, otherwise ad-hoc (-).
# Ad-hoc signing allows local testing without Apple Developer credentials.

APP_DIR="${1:-dist/SnapX.app}"
IDENTITY="${DEVELOPER_ID:--}"  # default to ad-hoc
ENTITLEMENTS="ShareXMac/SnapX.entitlements"

# Resolve to absolute paths
APP_DIR="$(cd "$(dirname "$APP_DIR")" && pwd)/$(basename "$APP_DIR")"
ENTITLEMENTS="$(cd "$(dirname "$ENTITLEMENTS")" && pwd)/$(basename "$ENTITLEMENTS")"

echo "Signing $APP_DIR"
echo "Identity: $IDENTITY"

# Work in /tmp to avoid iCloud/file-provider re-adding com.apple.FinderInfo
# which codesign rejects as "detritus"
TMP_APP="/tmp/$(basename "$APP_DIR")"
echo "Copying to $TMP_APP for clean signing environment..."
rm -rf "$TMP_APP"
ditto "$APP_DIR" "$TMP_APP"

# Strip any quarantine/Finder metadata on the copy
xattr -rc "$TMP_APP" 2>/dev/null || true

# Sign all Mach-O binaries first (inner-to-outer: dylibs before executables)
# .NET self-contained bundles have dylibs, the apphost executable, and PE32 DLLs
find "$TMP_APP" -type f | sort | while IFS= read -r f; do
    if file "$f" 2>/dev/null | grep -q "Mach-O"; then
        fname=$(basename "$f")
        if [ "$fname" != "SnapX" ]; then
            codesign --force --sign "$IDENTITY" \
                --options runtime \
                "$f" 2>/dev/null || true
        fi
    fi
done

# Sign PE32 DLLs (managed .NET assemblies — codesign requires them to be signed
# when they appear in the MacOS directory of a hardened runtime bundle)
find "$TMP_APP/Contents/MacOS" -name "*.dll" | sort | while IFS= read -r dll; do
    codesign --force --sign "$IDENTITY" "$dll" 2>/dev/null || true
done

# Sign any other non-Mach-O, non-DLL files in MacOS dir (e.g. JSON, HTML)
# codesign checks all files in MacOS for code-object compliance
find "$TMP_APP/Contents/MacOS" -type f \
    ! -name "*.dylib" \
    ! -name "*.so" \
    ! -name "*.dll" \
    ! -name "SnapX" | sort | while IFS= read -r f; do
    codesign --force --sign "$IDENTITY" "$f" 2>/dev/null || true
done

# Sign the main executable with entitlements
codesign --force --sign "$IDENTITY" \
    --options runtime \
    --entitlements "$ENTITLEMENTS" \
    "$TMP_APP/Contents/MacOS/SnapX"

# Sign the bundle
codesign --force --sign "$IDENTITY" \
    --options runtime \
    --entitlements "$ENTITLEMENTS" \
    "$TMP_APP"

echo "Verifying..."
codesign --verify --deep --strict "$TMP_APP" && echo "Signature valid"
codesign -dv --verbose=4 "$TMP_APP" 2>&1 | grep -E "^(Authority|TeamIdentifier|Identifier|Format)" || true

# Copy signed app back to original location
echo "Copying signed app back to $APP_DIR..."
rm -rf "$APP_DIR"
ditto "$TMP_APP" "$APP_DIR"
rm -rf "$TMP_APP"

echo "Done: $APP_DIR"
