:: Package Geneva SQE manager Service Fabric package

setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set SQE_MANAGER_CSPROJ_DIR=%REPO_ROOT_DIR%\src\Common\StandingQueryExtensionManagerApp
set DEST_DIR=%REPO_ROOT_DIR%\src\Deployment\Bin\StandingQueryExtensionManagerApp
set SOURCE_SFPKG=%REPO_ROOT_DIR%\src\Deployment\Bin\StandingQueryExtensionManagerApp.sfpkg

powershell -NoProfile -ExecutionPolicy Unrestricted -Command %SQE_MANAGER_CSPROJ_DIR%\Replace-Manifests.ps1 %SOURCE_SFPKG% %SQE_MANAGER_CSPROJ_DIR%\WorkerApplicationManifest.xml %SQE_MANAGER_CSPROJ_DIR%\ServiceManifest.xml %DEST_DIR%\WorkerStandingQueryExtensionManagerApp.sfpkg || exit /b 1
powershell -NoProfile -ExecutionPolicy Unrestricted -Command %SQE_MANAGER_CSPROJ_DIR%\Replace-Manifests.ps1 %SOURCE_SFPKG% %SQE_MANAGER_CSPROJ_DIR%\FrontdoorApplicationManifest.xml %SQE_MANAGER_CSPROJ_DIR%\ServiceManifest.xml %DEST_DIR%\FrontdoorStandingQueryExtensionManagerApp.sfpkg || exit /b 1

echo Everything is awesome! Bye.
exit /b 0
endlocal
