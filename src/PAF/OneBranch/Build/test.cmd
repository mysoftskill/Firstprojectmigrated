setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set UNITTEST_PS1="%REPO_ROOT_DIR%\.build\run_unittests.ps1"
set UNITTEST_DLL_DIR="%REPO_ROOT_DIR%\src\PAF\Product"
set UNITTEST_RUNSETTINGS="%REPO_ROOT_DIR%\.build\UnitTest.runsettings"

pushd %~dp0

echo powershell %UNITTEST_PS1% %UNITTEST_DLL_DIR% %UNITTEST_RUNSETTINGS%
powershell %UNITTEST_PS1% %UNITTEST_DLL_DIR% %UNITTEST_RUNSETTINGS%|| exit /b 1

popd

endlocal
echo Everything is awesome! Bye.
exit /b 0
