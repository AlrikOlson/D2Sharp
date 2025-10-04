param (
    [Parameter(Position=0)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

# Change to the current directory
Set-Location $PSScriptRoot
Write-Host "Current directory: $(Get-Location)"

# Set CGO_ENABLED to 1
$env:CGO_ENABLED = 1
Write-Host "CGO_ENABLED: $env:CGO_ENABLED"

# Determine the library extension based on the platform
if ($IsWindows) {
    $libExtension = "dll"
} elseif ($IsMacOS) {
    $libExtension = "dylib"
} elseif ($IsLinux) {
    $libExtension = "so"
} else {
    # Fallback to dll for backward compatibility
    $libExtension = "dll"
}

$libraryName = "d2wrapper.$libExtension"
Write-Host "Building for platform: $PSVersionTable.Platform, extension: $libExtension"

# Build the Go code into a shared library
Write-Host "Building Go code..."
go build -trimpath -buildmode=c-shared -buildvcs=false -ldflags "-s" -o $libraryName

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build Go code. Exit code: $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Go build completed successfully."

# Determine the target directory
$targetDir = "..\bin\$Configuration\net8.0"

Write-Host "Target directory: $targetDir"

# Create the target directory if it doesn't exist
Write-Host "Creating target directory if it doesn't exist..."
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

if (!(Test-Path $targetDir)) {
    Write-Error "Failed to create target directory: $targetDir"
    exit 1
}

Write-Host "Target directory created successfully."

# Copy the built shared library to the target directory
$sourceFile = $libraryName
$destinationFile = "$targetDir\$libraryName"

Write-Host "Copying shared library..."
Write-Host "Source file: $sourceFile"
Write-Host "Destination file: $destinationFile"

Copy-Item -Path $sourceFile -Destination $destinationFile -Force

if (!(Test-Path $destinationFile)) {
    Write-Error "Failed to copy shared library to destination: $destinationFile"
    exit 1
}

Write-Host "Shared library copied successfully."
