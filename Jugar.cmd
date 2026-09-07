@echo off
if not exist "%~dp0dist\NeonStack.exe" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
    if errorlevel 1 (
        pause
        exit /b 1
    )
)
start "" "%~dp0dist\NeonStack.exe"
