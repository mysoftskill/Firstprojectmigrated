:: Build PCF Solution
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

:: Build test hooks in test
if "%2"=="NONPROD" (
    set PCF_COMPILER_DEF="/p:CompileTestHooks=true"
)

:: Set developer environment for VS 2019
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 || exit /b 1

:: Build Validator
msbuild %PCF_COMPILER_DEF% /p:Configuration=%BUILD_CONFIG% /p:Platform=AnyCPU /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %REPO_ROOT_DIR%\src\PCF\Product\Libraries\PrivacyCommandValidator\PrivacyCommandValidator.csproj || exit /b 1
:: Build SDK
msbuild %PCF_COMPILER_DEF% /p:Configuration=%BUILD_CONFIG% /p:Platform=AnyCPU /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %REPO_ROOT_DIR%\src\PCF\Product\Client\Source\PrivacyCommandProcessor.csproj || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0