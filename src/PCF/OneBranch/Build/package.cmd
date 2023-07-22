:: Package PCF Azure Applications

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

set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\PCF\OneBranch

echo Preparation steps
rmdir /Q/S "%ONEBRANCH_DIR%\Release"
xcopy "%REPO_ROOT_DIR%\src\Deployment" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
xcopy "%ONEBRANCH_DIR%\Deployment" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
mkdir "%ONEBRANCH_DIR%\Release\Bin"

echo Building service fabric packages
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PCF PcfFrontdoorApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PCF PcfWorkerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PCF QueueDepthApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PCF PcfDataAgentApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PCF PcfAutoscalerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PCF WhatIfFilterAndRouteWorkItemHostApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_gma.cmd PCF || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0