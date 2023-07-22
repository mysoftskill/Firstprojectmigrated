@echo off

set AUTOPILOT_RELEASE_PATH=%1
set CATALOG_EXE_PATH=%2\CodeSign

if not exist "%AUTOPILOT_RELEASE_PATH%" (
    echo "Autopilot Release Path does not exist"
    exit /b 1
)

if not exist "%CATALOG_EXE_PATH%\DirSigner.exe" (
    echo "Unable to find DirSigner.exe"
    exit /b 1
)

if not exist "%CATALOG_EXE_PATH%\MakeCat.exe" (
    echo "Unable to find MakeCat.exe"
    exit /b 1
)

powershell.exe -File "%~dp0\Delete-EmptyFiles.ps1" -RootPath "%AUTOPILOT_RELEASE_PATH%"

set TMP_WAS_SUCCESS=true
set TMP_CAT_FAILURES=

for /D %%d in (%AUTOPILOT_RELEASE_PATH%\*) do (
    echo "Generating Catalog Files For %%d"
    "%CATALOG_EXE_PATH%\DirSigner.exe" -c true -n "" -d %%d
    if ERRORLEVEL 1 (
        set TMP_WAS_SUCCESS=false
        set TMP_CAT_FAILURES=%TMP_CAT_FAILURES%;%%d
    )
)

if "%TMP_WAS_SUCCESS%"=="true" (
    echo "Catalog Files Created"
    exit /b 0
)

echo "Failed to create catalog files for:"
echo %TMP_CAT_FAILURES%
exit /b 1