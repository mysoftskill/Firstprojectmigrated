:: Build PAF Solution
:: %1 parameters values: Debug and Release

setlocal
:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set BUILD_CONFIG=Release
if "%~1"=="" (
    set BUILD_CONFIG=Release
) else (
    set BUILD_CONFIG=%1
)

:: Set developer environment for VS 2019
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 

msbuild -ver
dotnet --list-sdks
set SOLUTION_FILE="%REPO_ROOT_DIR%\src\PAF\Product\PrivacyAzureFunctions.sln"
set PLATFORM=x64
set BUILD_CONFIG=Release
echo %BUILD_CONFIG%
:: disable warnaserror temporary, because new format of local.settings.json is not fully supported by Azure Functions SDK:
:: C:\Users\rupavlen\.nuget\packages\microsoft.net.sdk.functions\3.0.7\build\Microsoft.NE
::       T.Sdk.Functions.Build.targets(44,5): error : Function [TestAnaheimIdQueueFunction]: cann
::       ot find value named 'PAF_AID_STORAGE_ACCOUNT_NAME' in local.settings.json that matches '
::       connection' property set on 'queueTrigger' [D:\git\ComplianceServices\src\PAF\Product\Fu
::       nctions\Functions.csproj]
:: msbuild /warnaserror /p:ForcePackageTarget=true /p:Configuration=%BUILD_CONFIG% /p:Platform=%PLATFORM% /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %SOLUTION_FILE% || exit /b 1
msbuild /p:ForcePackageTarget=true /p:Configuration=%BUILD_CONFIG% /p:Platform=%PLATFORM% /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %SOLUTION_FILE% || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0
