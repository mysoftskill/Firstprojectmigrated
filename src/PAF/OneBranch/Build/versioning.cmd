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
set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\PAF\OneBranch

:: This is used to update the version on the release files of AID
powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& .\Update-Version.ps1 -Location %ONEBRANCH_DIR%\Deployment\AID -Version %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%" -ErrorAction Stop || exit /b 1
:: This is used to update the version on the release files of PAF
powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& .\Update-Version.ps1 -Location %ONEBRANCH_DIR%\Deployment\PAF -Version %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%" -ErrorAction Stop || exit /b 1
:: Update version in BuildVer.txt
echo %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS% > %ONEBRANCH_DIR%\Deployment\AID\BuildVer.txt
echo %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS% > %ONEBRANCH_DIR%\Deployment\PAF\BuildVer.txt
popd

endlocal
echo Everything is awesome! Bye.
exit /b 0
