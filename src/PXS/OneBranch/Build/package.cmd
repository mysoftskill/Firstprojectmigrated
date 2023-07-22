:: Package PXS Azure Applications
:: %1 parameters values: Debug and Release

setlocal
:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set BUILD_CONFIG=Debug
if "%~1"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%1
)

set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\PXS\OneBranch

echo Preparation steps
rmdir /Q/S "%ONEBRANCH_DIR%\Release"
xcopy "%REPO_ROOT_DIR%\src\Deployment" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
xcopy "%REPO_ROOT_DIR%\src\Deployment\GenevaSynthetics" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
xcopy "%ONEBRANCH_DIR%\Deployment" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
mkdir "%ONEBRANCH_DIR%\Release\Bin"

echo Building geneva synthetics agent package
call %REPO_ROOT_DIR%\.build\package_syn.cmd PXS || exit /b 1

echo Building service fabric packages
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS DataActionRunnerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS PxsServiceFabricApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS VortexDeviceDeleteWorkerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS AadAccountCloseWorkerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS PrivacyAqsWorkerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS PrivacyVsoWorkerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS QuickExportWorkerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS CosmosExportWorkerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS PxsPartnerMockApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS MsaAgeOutFakeCommandWorkerApp %BUILD_CONFIG% || exit /b 1
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PXS RecurrentDeleteWorkerApp %BUILD_CONFIG% || exit /b 1

echo Building geneva monitoring agent package
call %REPO_ROOT_DIR%\.build\package_gma.cmd PXS || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0
