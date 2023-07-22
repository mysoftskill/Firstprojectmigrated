:: Build PXS Client project
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

:: Set developer environment for VS 2019
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 || exit /b 1

:: Build PXS Client
msbuild /p:Configuration=%BUILD_CONFIG% /p:Platform=x64 /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %REPO_ROOT_DIR%\src\PXS\Product\PXF\PrivacyExperienceClientLibrary\Source\PrivacyExperienceClientLibrary.csproj || exit /b 1

:: Build PXS Command Contracts
msbuild %PCF_COMPILER_DEF% /p:Configuration=%BUILD_CONFIG% /p:Platform=x64 /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %REPO_ROOT_DIR%\src\PXS\Product\Contracts\PCFContracts\PXS.Command.Contracts.csproj || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0