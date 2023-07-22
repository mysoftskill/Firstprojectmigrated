:: Package Azure Service Fabric Applications
:: Compress the prebuilt GenevaMonitoringAgentConsoleApp to a zip file and publish it to release folder

if "%~1"=="" (
    echo Project name is not specified
    goto :usage
) else (
    set PROJECT_NAME=%1
)

setlocal
:: Set GMA Version
set GMA_VERSION=8.0.1565.4

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\%PROJECT_NAME%\OneBranch
set RELEASE_DIR=%ONEBRANCH_DIR%\Release
set COMPRESS_PS1=%REPO_ROOT_DIR%\.build\CompressArchive.ps1
set SFAPP_NAME=GenevaMonitoringAgentConsoleApp

echo Repository root dir: %REPO_ROOT_DIR%

set SFPKG_DIR=%REPO_ROOT_DIR%\src\Common\%SFAPP_NAME%\Drop\Monitoring\%SFAPP_NAME%

:: Update Ev2 Rollout App Version
powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& %REPO_ROOT_DIR%\.build\Update-PackageVersion.ps1 -PackageLocation %SFPKG_DIR% -Version %GMA_VERSION%" -ErrorAction Stop || exit /b 1
powershell "%COMPRESS_PS1%" "%SFPKG_DIR%\*" "%RELEASE_DIR%\Bin\%SFAPP_NAME%.zip" || exit /b 1
:: Rename .zip to .sfpkg
move /Y "%RELEASE_DIR%\Bin\%SFAPP_NAME%.zip" "%RELEASE_DIR%\Bin\%SFAPP_NAME%.sfpkg" || exit /b 1

echo Everything is awesome! Bye.
exit /b 0
endlocal

:usage
    echo package.cmd <PROJECT_NAME>
    echo PROJECT_NAME: PCF, PXS
