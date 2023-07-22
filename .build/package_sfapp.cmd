:: Package Azure Service Fabric Applications
:: Compress ApplicationPackageRoot to a zip file and publish it into release folder

if "%~1"=="" (
    echo Project name is not specified
    goto :usage
) else (
    set SOURCE_PROJECT_NAME=%1
    set RELEASE_PROJECT_NAME=%1
)

if "%~2"=="" (
    echo SF App name is not specified
    goto :usage
) else (
    set SFAPP_NAME=%2
)

if "%~3"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%3
)

:: Optional parameter to redirect build outputs to a different release folder.
:: For instance, a few SF Apps in PXS are redirected to NGPProxy for release
if not "%~4"=="" (
    set RELEASE_PROJECT_NAME=%4
)

setlocal
:: Set fake version if does not exist
if "%CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%"=="" (
    set CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS=0.0.851.2
)

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set PRODUCT_DIR=%REPO_ROOT_DIR%\src\%SOURCE_PROJECT_NAME%\Product
set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\%RELEASE_PROJECT_NAME%\OneBranch
set RELEASE_DIR=%ONEBRANCH_DIR%\Release
set COMPRESS_PS1=%REPO_ROOT_DIR%\.build\CompressArchive.ps1
set SFPKG_APP_DIR=%PRODUCT_DIR%\Azure\%SFAPP_NAME%

echo Repository root dir: %REPO_ROOT_DIR%

powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& %REPO_ROOT_DIR%\.build\Update-PackageVersion.ps1 -PackageLocation %SFPKG_APP_DIR%\pkg\%BUILD_CONFIG% -Version %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%" -ErrorAction Stop || exit /b 1

:: Compress to zip file
powershell "%COMPRESS_PS1%" "%SFPKG_APP_DIR%\pkg\%BUILD_CONFIG%\*" "%RELEASE_DIR%\Bin\%SFAPP_NAME%.zip" || exit /b 1
:: Rename .zip to .sfpkg
move /Y "%RELEASE_DIR%\Bin\%SFAPP_NAME%.zip" "%RELEASE_DIR%\Bin\%SFAPP_NAME%.sfpkg" || exit /b 1

echo Everything is awesome! Bye.
exit /b 0
endlocal

:usage
    echo package.cmd <SOURCE_PROJECT_NAME> <SFAPP_NAME> <BUILD_CONFIG> <RELEASE_PROJECT_NAME>
    echo SOURCE_PROJECT_NAME: PCF, PXS
    echo SFAPP_NAME: Service Fabric App (folder) name, e.g. PcfFrontdoorApp
    echo BUILD_CONFIG: Debug, Release
    echo (Optional) RELEASE_PROJECT_NAME: Redirect the source project build outputs to a different release folder