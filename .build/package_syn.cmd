:: Package Geneva Synthetics Applications
:: Compress the synthetics test apps to a zip file and publish it to release folder

set SYN_ARCHIVE_NAME=SyntheticsTests

if "%~1"=="" (
    echo Project name is not specified
    goto :usage
) else (
    set SOURCE_PROJECT_NAME=%1
    set RELEASE_PROJECT_NAME=%1
)

if "%~2"=="" (
    set SYN_CONFIG_NAME=SyntheticsJobGroup.json
) else (
    set SYN_CONFIG_NAME=%2
)

if "%~3"=="" (
    set SYN_PKG_DIR=%REPO_ROOT_DIR%\src\%SOURCE_PROJECT_NAME%\Bin\%BUILD_CONFIG%\x64\%SYN_ARCHIVE_NAME%
) else (
    set SYN_PKG_DIR=%REPO_ROOT_DIR%\src\%SOURCE_PROJECT_NAME%\%3
)


setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set SYN_ARCHIVE_NAME=SyntheticsTests
set RELEASE_DIR=%REPO_ROOT_DIR%\src\%RELEASE_PROJECT_NAME%\OneBranch\Release
set COMPRESS_PS1=%REPO_ROOT_DIR%\.build\CompressArchive.ps1

echo Repository root dir: %REPO_ROOT_DIR%

:: Copy the Synthetics App Configuration File
xcopy "%SYN_PKG_DIR%\%SYN_CONFIG_NAME%" "%RELEASE_DIR%\Bin\" /Q /Y /F || exit /b 1

:: Create Zip Archive of the Synthetics Apps and Dependencies
powershell "%COMPRESS_PS1%" "%SYN_PKG_DIR%\*" "%RELEASE_DIR%\Bin\%SYN_ARCHIVE_NAME%.zip" || exit /b 1

echo Everything is awesome! Bye.
exit /b 0
endlocal

:usage
    echo package_syn.cmd <PROJECT_NAME>
    echo PROJECT_NAME: PXS