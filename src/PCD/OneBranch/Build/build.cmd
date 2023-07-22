:: Build PCD Solution
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
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64

set SOLUTION_FILE="%REPO_ROOT_DIR%\src\PCD\pdmsux.sln"
:: First build sets up the wwwroot css and js files to be copied from SF
call "%REPO_ROOT_DIR%\src\PCD\Product\Build\buildall.cmd" || exit /b 1

call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64
:: Build the solution
:: The msbuild pulls in all dependencies in the csproj files, but deletes the css and js files
:: msbuild /p:ForcePackageTarget=true /p:Configuration=%BUILD_CONFIG% /p:Platform=x64 /m /flp:LogFile=build.log;WarningsOnly;ErrorsOnly %SOLUTION_FILE% || exit /b 1
:: This second build restores the css and js files to be used in testing
call "%REPO_ROOT_DIR%\src\PCD\Product\Build\buildall.cmd" || exit /b 1
endlocal

echo Everything is awesome! Bye.
exit /b 0
