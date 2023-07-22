:: Run unittests

setlocal

set BUILD_CONFIG=Debug
if "%~1"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%1
)

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set UNITTEST_PS1="%REPO_ROOT_DIR%\.build\run_unittests.ps1"
set UNITTEST_DLL_DIR="%REPO_ROOT_DIR%\src\PXS\Bin\%BUILD_CONFIG%\x64"

pushd %~dp0

echo powershell %UNITTEST_PS1% %UNITTEST_DLL_DIR%
powershell %UNITTEST_PS1% %UNITTEST_DLL_DIR%|| exit /b 1

popd

endlocal
echo Everything is awesome! Bye.
exit /b 0