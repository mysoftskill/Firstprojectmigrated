setlocal
::set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

:: Update version
set VersionNumber=%1
if "%~1"=="" (
    echo "No version number is given from onebranch step!"
    exit /b 1
)

pushd "%REPO_ROOT_DIR%\.build"
set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\PXS\OneBranch

:: Update ASF App Version
powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& .\Update-Version-OneBranch.ps1 -Location "%ONEBRANCH_DIR%\Deployment" -Version %VersionNumber% -OneBranchDir %ONEBRANCH_DIR%" -ErrorAction Stop || exit /b 1

:: Update version in BuildVer.txt
::echo %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS% > %ONEBRANCH_DIR%\Deployment\BuildVer.txt
popd

::endlocal
echo Everything is awesome! Bye.
exit /b 0