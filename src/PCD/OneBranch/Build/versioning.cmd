:: Update version
echo Release Version: %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%

setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

if "%CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%"=="" (
    set CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS=0.0.851.2
)

pushd "%REPO_ROOT_DIR%\.build"
set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\PCD\OneBranch

:: versioning command here
powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& .\Update-Version-OneBranch.ps1 -Location "%ONEBRANCH_DIR%\Deployment" -Version %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS% -OneBranchDir %ONEBRANCH_DIR%" -ErrorAction Stop || exit /b 1
popd

endlocal
echo Everything is awesome! Bye.
exit /b 0
