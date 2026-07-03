@echo off
setlocal

rem Writes your current in-game shader config (shaders.Config.txt) into the Atmo/Clouds shader
rem source's hardcoded defaults. Run this after tuning values in-game so the new numbers become
rem the baked-in defaults for the next shader pack build.
rem
rem Usage:
rem   sync-shader-config.bat
rem   sync-shader-config.bat "D:\custom\path\to\shaders.Config.txt"

if "%~1"=="" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sync-shader-config.ps1"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sync-shader-config.ps1" -ConfigPath "%~1"
)

endlocal
