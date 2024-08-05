# check_prerequisites.ps1

# Function to check if a command is available
function Test-Command($command) {
    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = 'stop'
    try { if (Get-Command $command) { return $true } }
    catch { return $false }
    finally { $ErrorActionPreference = $oldPreference }
}

# Function to check .NET SDK version
function Check-DotNetSDK {
    if (Test-Command "dotnet") {
        $version = (dotnet --version)
        if ([version]$version -ge [version]"8.0") {
            Write-Host ".NET SDK $version is installed." -ForegroundColor Green
        } else {
            Write-Host ".NET SDK 8.0 or later is required. Current version: $version" -ForegroundColor Red
        }
    } else {
        Write-Host ".NET SDK is not installed." -ForegroundColor Red
    }
}

# Function to check Go version
function Check-Go {
    if (Test-Command "go") {
        $version = (go version) -replace "go version go" -split " " | Select-Object -First 1
        if ([version]$version -ge [version]"1.22.2") {
            Write-Host "Go $version is installed." -ForegroundColor Green
        } else {
            Write-Host "Go 1.22.2 or later is required. Current version: $version" -ForegroundColor Red
        }
    } else {
        Write-Host "Go is not installed." -ForegroundColor Red
    }
}

# Function to check 64-bit GCC
function Check-GCC64 {
    if (Test-Command "gcc") {
        $gccOutput = gcc --version
        $versionLine = $gccOutput | Select-Object -First 1
        if ($versionLine -match "tdm64") {
            $version = $versionLine -replace "gcc\.exe \(tdm64-\d+\) "
            Write-Host "64-bit TDM-GCC $version is installed." -ForegroundColor Green
        } elseif ($gccOutput | Select-String "Target: x86_64") {
            $version = $versionLine -replace "gcc \(.*\) "
            Write-Host "64-bit GCC $version is installed." -ForegroundColor Green
        } else {
            Write-Host "64-bit GCC is required. Found: $versionLine" -ForegroundColor Red
        }
    } else {
        Write-Host "GCC is not installed." -ForegroundColor Red
    }
}

# Main script
Write-Host "Checking prerequisites for d2.Net project..." -ForegroundColor Cyan

Check-DotNetSDK
Check-Go
Check-GCC64

Write-Host "`nPrerequisite check completed." -ForegroundColor Cyan
