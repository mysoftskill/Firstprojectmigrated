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

:: Publish Custom activity
dotnet publish %REPO_ROOT_DIR%\src\PCF\Product\Client\PrivacyCommandCustomActivity\PrivacyCommandCustomActivity.csproj -r win-x64 -p:PublishSingleFile=true -p:Configuration=%BUILD_CONFIG% -p:Platform=AnyCPU --self-contained true || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0