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
set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\PCF\OneBranch

:: Update ASF App Version
powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& .\Update-Version.ps1 -Location "%ONEBRANCH_DIR%\Deployment" -Version %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%" -ErrorAction Stop || exit /b 1

:: Update version in BuildVer.txt
echo %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS% > %ONEBRANCH_DIR%\Deployment\BuildVer.txt
popd

endlocal
echo Everything is awesome! Bye.
exit /b 0