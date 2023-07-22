:: Run All unittests

setlocal
:: set repo root folder
set REPO_ROOT_DIR=%~dp0\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

call %REPO_ROOT_DIR%\src\PCF\OneBranch\Build\test.cmd || exit /b 1
call %REPO_ROOT_DIR%\src\PXS\OneBranch\Build\test.cmd || exit /b 1
endlocal

exit /b 0