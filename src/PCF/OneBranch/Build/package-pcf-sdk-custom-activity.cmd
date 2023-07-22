:: Restore NuGet packages

setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%

:: stub for cdpx build version. set your version here if running outside the cdpx.
if "%BUILD_BUILDNUMBER%"=="" (
    set BUILD_BUILDNUMBER=0.0.851.2
)

:: set -pre suffix for non-prod releases.
set PKG_VERSION=%BUILD_BUILDNUMBER%
if not "%~1"=="PROD" (
    set PKG_VERSION=%PKG_VERSION%-pre
)

set BUILD_CONFIG=Debug
if "%~2"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%2
)

:: Pack Custom Activity
set CA_PACKAGE_SOURCE_DIR=%REPO_ROOT_DIR%\src\PCF\bin\%BUILD_CONFIG%\x64\PrivacyCommandCustomActivity\win-x64\publish
powershell -NoProfile -ExecutionPolicy Unrestricted -Command "& New-Item -ItemType Directory -Force -Path %REPO_ROOT_DIR%\src\PCF\nupkgs"
powershell -NoProfile -ExecutionPolicy Unrestricted -Command "& %REPO_ROOT_DIR%\src\PCF\OneBranch\Build\PackPcfCustomActivity.ps1 %CA_PACKAGE_SOURCE_DIR% %REPO_ROOT_DIR%\src\PCF\nupkgs\PrivacyCommandCustomActivity.zip" -ErrorAction Stop || exit /b 1

popd
endlocal
echo Everything is awesome! Bye.
exit /b 0