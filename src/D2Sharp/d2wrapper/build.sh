#!/bin/bash
set -e

# Parse configuration argument (default to Debug)
CONFIGURATION="${1:-Debug}"

# Validate configuration
if [[ "$CONFIGURATION" != "Debug" && "$CONFIGURATION" != "Release" ]]; then
    echo "Error: Configuration must be Debug or Release"
    exit 1
fi

# Change to the script's directory
cd "$(dirname "$0")"
echo "Current directory: $(pwd)"

# Set CGO_ENABLED to 1
export CGO_ENABLED=1
echo "CGO_ENABLED: $CGO_ENABLED"

# Determine the library extension based on the platform
case "$(uname -s)" in
    Linux*)
        LIB_EXTENSION="so"
        ;;
    Darwin*)
        LIB_EXTENSION="dylib"
        ;;
    CYGWIN*|MINGW*|MSYS*)
        LIB_EXTENSION="dll"
        ;;
    *)
        echo "Warning: Unknown platform, defaulting to .so"
        LIB_EXTENSION="so"
        ;;
esac

LIBRARY_NAME="d2wrapper.$LIB_EXTENSION"
echo "Building for platform: $(uname -s), extension: $LIB_EXTENSION"

# Build the Go code into a shared library
echo "Building Go code..."
go build -trimpath -buildmode=c-shared -buildvcs=false -ldflags "-s" -o "$LIBRARY_NAME"

if [ $? -ne 0 ]; then
    echo "Error: Failed to build Go code"
    exit 1
fi

echo "Go build completed successfully."

# Determine the target directory
TARGET_DIR="../bin/$CONFIGURATION/net8.0"

echo "Target directory: $TARGET_DIR"

# Create the target directory if it doesn't exist
echo "Creating target directory if it doesn't exist..."
mkdir -p "$TARGET_DIR"

if [ ! -d "$TARGET_DIR" ]; then
    echo "Error: Failed to create target directory: $TARGET_DIR"
    exit 1
fi

echo "Target directory created successfully."

# Copy the built shared library to the target directory
SOURCE_FILE="$LIBRARY_NAME"
DESTINATION_FILE="$TARGET_DIR/$LIBRARY_NAME"

echo "Copying shared library..."
echo "Source file: $SOURCE_FILE"
echo "Destination file: $DESTINATION_FILE"

cp -f "$SOURCE_FILE" "$DESTINATION_FILE"

if [ ! -f "$DESTINATION_FILE" ]; then
    echo "Error: Failed to copy shared library to destination: $DESTINATION_FILE"
    exit 1
fi

echo "Shared library copied successfully."
