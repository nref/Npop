#!/bin/bash

APP_NAME="npop"
EXECUTABLE_NAME="npop"

rm -rf "bin/Debug/net8.0/$APP_NAME.app"

# Create the bundle directory structure
mkdir -p "$APP_NAME.app/Contents/MacOS"

# Create the Info.plist file
cat > "$APP_NAME.app/Contents/Info.plist" <<EOL
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>$EXECUTABLE_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>com.example.$APP_NAME</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
</dict>
</plist>
EOL

# Move the compiled executable to the bundle
cp -r "bin/Debug/net8.0/" "$APP_NAME.app/Contents/MacOS/"

# Make the executable file executable
chmod +x "$APP_NAME.app/Contents/MacOS/$EXECUTABLE_NAME"

mv "$APP_NAME.app" "bin/Debug/net8.0"
