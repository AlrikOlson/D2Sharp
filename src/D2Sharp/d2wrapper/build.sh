#!/bin/bash
set -e

# Change to the current directory
cd "$(dirname "$0")"

# Set CGO_ENABLED to 1
export CGO_ENABLED=1

# Build the Go code into a shared library
go build -trimpath -buildmode=c-shared -ldflags "-s" -o "d2wrapper.so"

# Get the configuration from the command-line argument
CONFIGURATION=${1:-Debug}

# Determine the target directory based on the operating system
case "$(uname -s)" in
    CYGWIN*|MINGW32*|MSYS*|MINGW*)
        # Windows
        TARGET_DIR="../bin/${CONFIGURATION}/net8.0"
        TARGET_FILE="d2wrapper.dll"
        ;;
    *)
        # Unix-based systems
        TARGET_DIR="../bin/${CONFIGURATION}/net8.0"
        TARGET_FILE="d2wrapper.so"
        ;;
esac

# Create the target directory if it doesn't exist
mkdir -p "${TARGET_DIR}"

# Copy the built shared library to the target directory
cp "d2wrapper.so" "${TARGET_DIR}/${TARGET_FILE}"
