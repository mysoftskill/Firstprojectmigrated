@echo off

setlocal enabledelayedexpansion

pushd "%~dp0"

where npm.cmd 1>nul
if %errorlevel%==1 (
    echo ERROR: Node.js/NPM was not found.
    goto :exit
)

powershell -ExecutionPolicy ByPass -File .\Setup-Devbox.ps1 %*

echo All done!

:exit

popd
