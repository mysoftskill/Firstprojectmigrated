:: Restore NuGet packages

setlocal
:: set repo root folder
set REPO_ROOT_DIR=%~dp0..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

call %REPO_ROOT_DIR%\src\NGPProxy\OneBranch\Build\restore.cmd || exit /b 1
call %REPO_ROOT_DIR%\src\PAF\OneBranch\Build\restore.cmd || exit /b 1
:: call %REPO_ROOT_DIR%\src\PCD\OneBranch\Build\restore.cmd || exit /b 1
call %REPO_ROOT_DIR%\src\PCF\OneBranch\Build\restore.cmd || exit /b 1
call %REPO_ROOT_DIR%\src\PDMS\OneBranch\Build\restore.cmd || exit /b 1
call %REPO_ROOT_DIR%\src\PXS\OneBranch\Build\restore.cmd || exit /b 1

endlocal

exit /b 0