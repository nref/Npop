#!/bin/bash

#APP_NAME="./bin/Debug/net8.0/npop.app"
APP_NAME="./bin/Release/net8.0-macos/osx-arm64/npop.macOS.app"
ENTITLEMENTS="Entitlements.plist"
SIGNING_IDENTITY="Developer ID Application: Carl Slater (D89E59Y3DZ)"
MAX_CORES=8

# Function to sign a file
sign_file() {
    local fname="$1"
    echo "[INFO] Signing $fname"
    codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
}

export -f sign_file
export ENTITLEMENTS
export SIGNING_IDENTITY

# Find all files to sign in parallel
find "$APP_NAME/Contents/MacOS" "$APP_NAME/Contents/MonoBundle" -type f | xargs -P "$MAX_CORES" -n 1 bash -c 'sign_file "$0"'

echo "[INFO] Signing app file"
sign_file "$APP_NAME"
