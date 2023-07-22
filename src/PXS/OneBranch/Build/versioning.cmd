:: Update version
echo Release Version: %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%

setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

if "%CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%"=="" (
    set CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS=1.1.1.1
)

pushd "%REPO_ROOT_DIR%\.build"
:: Update ASF App Version
powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& .\Update-Version.ps1 -Location %REPO_ROOT_DIR%\src\PXS\OneBranch\Deployment -Version %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%" || exit /b 1

popd

endlocal
echo Everything is awesome! Bye.
exit /b 0