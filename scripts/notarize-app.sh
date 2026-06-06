#!/usr/bin/env bash
set -euo pipefail

# Usage: bash scripts/notarize-app.sh [app-path] [api-key-path]
# Requires: AC_API_KEY_ID and AC_API_ISSUER_ID environment variables
# api-key-path: path to the .p8 App Store Connect API key file

APP_DIR="${1:-dist/SnapX.app}"
API_KEY_PATH="${2:?API key path argument required (usage: notarize-app.sh [app-path] [api-key-path])}"

ZIP_PATH="${APP_DIR%.app}-notarize.zip"

echo "Zipping $APP_DIR for submission..."
ditto -c -k --keepParent "$APP_DIR" "$ZIP_PATH"

echo "Submitting for notarization (this takes 1-5 minutes)..."
xcrun notarytool submit "$ZIP_PATH" \
    --key "$API_KEY_PATH" \
    --key-id "${AC_API_KEY_ID:?}" \
    --issuer "${AC_API_ISSUER_ID:?}" \
    --wait

rm -f "$ZIP_PATH"

echo "Stapling ticket..."
xcrun stapler staple "$APP_DIR"
xcrun stapler validate "$APP_DIR" && echo "Staple valid"

echo "Notarized and stapled: $APP_DIR"
