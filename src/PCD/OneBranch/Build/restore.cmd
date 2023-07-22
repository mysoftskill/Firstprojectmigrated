:: Restore NuGet packages

setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

rem install chrome to run headless chrome unit tests unless installed on image
if NOT exist "%ProgramFiles(x86)%\Google\Chrome" (
    powershell -File "%REPO_ROOT_DIR%\src\PCD\OneBranch\Build\install_chrome.ps1"
    )

rem installs fonts needed to run chrome
powershell -NoProfile -ExecutionPolicy Unrestricted -Command "& '%REPO_ROOT_DIR%\src\PCD\Product\Build\Add-Font.ps1' %REPO_ROOT_DIR%\src\PCD\Product\Build\fonts"

:: setup npm auth for microsoft artifacts
call npm install -g vsts-npm-auth
call vsts-npm-auth -config .npmrc

call "%REPO_ROOT_DIR%\src\PCD\Product\Build\restoreall.cmd" || exit /b 1

set SOLUTION_FILE="%REPO_ROOT_DIR%\src\PCD\pdmsux.sln"


::dotnet restore %SOLUTION_FILE%

:: rem copy dependencies since GenevaMonitoringAgent uses references to src\NugetPackages
:: if  exist "%REPO_ROOT_DIR%\src\NugetPackages" (
::     rmdir /Q/S "%REPO_ROOT_DIR%\src\NugetPackages"
:: )
:: mkdir "%REPO_ROOT_DIR%\src\NugetPackages"
:: xcopy /s "%REPO_ROOT_DIR%\src\PCD\packages" "%REPO_ROOT_DIR%\src\NugetPackages" >NUL

endlocal
echo Everything is awesome! Bye.
exit /b 0
