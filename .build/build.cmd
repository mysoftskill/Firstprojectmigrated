:: Build All
:: %1 parameters values: Debug and Release

setlocal
:: set repo root folder
set REPO_ROOT_DIR=%~dp0..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set BUILD_CONFIG=Debug
if "%~1"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%1
)

call %REPO_ROOT_DIR%\src\NGPProxy\OneBranch\Build\build.cmd %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\src\PAF\OneBranch\Build\build.cmd %BUILD_CONFIG% || exit /b 1
:: call %REPO_ROOT_DIR%\src\PCD\OneBranch\Build\build.cmd %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\src\PCF\OneBranch\Build\build.cmd %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\src\PDMS\OneBranch\Build\build.cmd %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\src\PXS\OneBranch\Build\build.cmd %BUILD_CONFIG% || exit /b 1

endlocal
exit /b 0