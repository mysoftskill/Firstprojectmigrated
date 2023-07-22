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
set UNITTEST_DLL_DIR="%REPO_ROOT_DIR%\src\PCF\bin\%BUILD_CONFIG%\x64"
set UNITTEST_RUNSETTINGS="%REPO_ROOT_DIR%\.build\UnitTest.runsettings"

pushd %~dp0

:: Run unittests
echo powershell %UNITTEST_PS1% %UNITTEST_DLL_DIR% %UNITTEST_RUNSETTINGS%
powershell -NoProfile -ExecutionPolicy Unrestricted -Command %UNITTEST_PS1% %UNITTEST_DLL_DIR% %UNITTEST_RUNSETTINGS% || exit /b 1

popd

endlocal
echo Everything is awesome! Bye.
exit /b 0
