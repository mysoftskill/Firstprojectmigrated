:: Build PDMS Client project
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

:: Set developer environment for VS 2022
call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 || exit /b 1

:: Build PDMS Client
msbuild /p:Configuration=%BUILD_CONFIG% /p:Platform=x64 /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %REPO_ROOT_DIR%\src\PDMS\Product\ClientSdks\Client\Pdms.Client.csproj || exit /b 1
:: Build PDMS Client AAD
msbuild /p:Configuration=%BUILD_CONFIG% /p:Platform=x64 /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %REPO_ROOT_DIR%\src\PDMS\Product\ClientSdks\Pdms.Client.Aad\Pdms.Client.Aad.csproj || exit /b 1
:: Build PDMS Client ServiceTree
msbuild /p:Configuration=%BUILD_CONFIG% /p:Platform=x64 /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %REPO_ROOT_DIR%\src\PDMS\Product\ClientSdks\ServiceTree\ServiceTree.Client.csproj || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0