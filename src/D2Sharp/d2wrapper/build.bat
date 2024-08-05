@echo off
cd /d %~dp0
set CGO_ENABLED=1
go build -trimpath -buildmode=c-shared -ldflags "-s" -o "d2wrapper.dll"

REM Get the configuration from the command-line argument
set CONFIGURATION=%1
if "%CONFIGURATION%"=="" (
    set CONFIGURATION=Debug
)

REM Copy the built dll to the D2Sharp project based on the configuration
xcopy /y /d "d2wrapper.dll" "..\bin\%CONFIGURATION%\net8.0\"
