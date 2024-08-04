#!/bin/bash

# Function to check if a command is available
check_command() {
    if command -v $1 >/dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to check .NET SDK version
check_dotnet_sdk() {
    if check_command dotnet; then
        version=$(dotnet --version)
        if [[ $(echo $version | cut -d. -f1) -ge 8 ]]; then
            echo -e "\033[0;32m.NET SDK $version is installed.\033[0m"
        else
            echo -e "\033[0;31m.NET SDK 8.0 or later is required. Current version: $version\033[0m"
        fi
    else
        echo -e "\033[0;31m.NET SDK is not installed.\033[0m"
    fi
}

# Function to check Go version
check_go() {
    if check_command go; then
        version=$(go version | awk '{print $3}' | sed 's/go//')
        if [[ $(echo $version | cut -d. -f1,2) == "1.22" ]] || [[ $(echo $version | cut -d. -f1,2) > "1.22" ]]; then
            echo -e "\033[0;32mGo $version is installed.\033[0m"
        else
            echo -e "\033[0;31mGo 1.22.2 or later is required. Current version: $version\033[0m"
        fi
    else
        echo -e "\033[0;31mGo is not installed.\033[0m"
    fi
}

# Function to check GCC
check_gcc() {
    if check_command gcc; then
        version=$(gcc --version | head -n1 | awk '{print $NF}')
        echo -e "\033[0;32mGCC $version is installed.\033[0m"
    else
        echo -e "\033[0;31mGCC is not installed.\033[0m"
    fi
}

# Main script
echo "Checking prerequisites for d2.Net project..."
echo

check_dotnet_sdk
check_go
check_gcc

echo
echo "Prerequisite check completed."
