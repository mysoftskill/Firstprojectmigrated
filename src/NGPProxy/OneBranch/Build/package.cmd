:: Package NGPProxy Azure Applications
:: %1 parameters values: Debug or Release

setlocal
:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
set ONEBRANCH_DIR=%~dp0..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set BUILD_CONFIG=Debug
if "%~1"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%1
)

echo Preparation steps
rmdir /Q/S "%ONEBRANCH_DIR%\Release"
xcopy "%REPO_ROOT_DIR%\src\Deployment" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
xcopy "%ONEBRANCH_DIR%\Deployment" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
mkdir "%ONEBRANCH_DIR%\Release\Bin"

echo Package Azure Applications
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS AadAccountCloseWorkerApp %BUILD_CONFIG% NGPProxy || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd NGPProxy PcfDataAgentApp %BUILD_CONFIG% NGPProxy || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd NGPProxy PcfDataAgentV2App %BUILD_CONFIG% NGPProxy || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd NGPProxy PxsServiceFabricApp %BUILD_CONFIG% NGPProxy || exit /b 1
call %REPO_ROOT_DIR%\.build\package_gma.cmd NGPProxy || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0