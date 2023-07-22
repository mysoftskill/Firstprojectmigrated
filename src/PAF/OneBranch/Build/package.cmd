:: Package PAF Azure Applications

setlocal
:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\PAF\OneBranch

echo Preparation steps
if exist "%ONEBRANCH_DIR%\Release" (
    rmdir /Q/S "%ONEBRANCH_DIR%\Release"
    )

:: param1: release folder name
:: param2: AzureFunction Project name
:: param3: Compressed file name
call %ONEBRANCH_DIR%\Build\package_paf.cmd PAF Functions WorkItemProcessor || exit /b 1
call %ONEBRANCH_DIR%\Build\package_paf.cmd AID AIdFunctions AnaheimIdProcessor || exit /b 1

:: TODO: Update version if applicable to azure functions
endlocal

echo Everything is awesome! Bye.
exit /b 0
