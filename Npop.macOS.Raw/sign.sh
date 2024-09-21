#!/bin/bash
APP_NAME="./bin/Debug/net8.0/npop.app"
ENTITLEMENTS="npop.entitlements"
SIGNING_IDENTITY="Developer ID Application: Carl Slater (D89E59Y3DZ)"

find "$APP_NAME/Contents/MacOS"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done

echo "[INFO] Signing app file"

codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$APP_NAME"
