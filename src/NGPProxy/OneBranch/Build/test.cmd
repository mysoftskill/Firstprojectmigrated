:: Run UnitTests

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
set NGPPROXY_UNITTEST_DLL_DIR="%REPO_ROOT_DIR%\src\NGPProxy\bin\%BUILD_CONFIG%\x64"
set UNITTEST_RUNSETTINGS="%REPO_ROOT_DIR%\.build\UnitTest.runsettings"

:: All referenced PXS projects are still built in its original location so running those tests from PXS folder.
set PXS_UNITTEST_DLL_DIR="%REPO_ROOT_DIR%\src\PXS\Bin\%BUILD_CONFIG%\x64"

pushd %~dp0

:: Uncomment this section when there are unit tests for NGPProxy
:: Run unittests
:: echo powershell %UNITTEST_PS1% % NGPPROXY_UNITTEST_DLL_DIR%
:: powershell -NoProfile -ExecutionPolicy Unrestricted -Command %UNITTEST_PS1% % NGPPROXY_UNITTEST_DLL_DIR% || exit /b 1

echo powershell %UNITTEST_PS1% %PXS_UNITTEST_DLL_DIR% %UNITTEST_RUNSETTINGS%
powershell -NoProfile -ExecutionPolicy Unrestricted -Command %UNITTEST_PS1% %PXS_UNITTEST_DLL_DIR% %UNITTEST_RUNSETTINGS%|| exit /b 1

popd

endlocal
echo Everything is awesome! Bye.
exit /b 0
