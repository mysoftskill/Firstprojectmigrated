:: Package NuGet packages

setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%

:: stub for cdpx build version. set your version here if running outside the cdpx.
if "%CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%"=="" (
    set CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS=0.0.19274.1
)

:: set -pre suffix for non-prod releases.
set PKG_VERSION=%CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%
if not "%~1"=="PROD" (
    set PKG_VERSION=%PKG_VERSION%-pre
)

set PKG_CONFIG=DEBUG
if "%~2"=="" (
    set PKG_CONFIG=DEBUG
) else (
    set PKG_CONFIG=%2
)

dotnet pack %REPO_ROOT_DIR%\src\PDMS\Product\ClientSdks\Client\Pdms.Client.csproj -p:PackageVersion=%PKG_VERSION% -c %PKG_CONFIG% --no-build --output %REPO_ROOT_DIR%\src\PDMS\nupkgs || exit /b 1
dotnet pack %REPO_ROOT_DIR%\src\PDMS\Product\ClientSdks\Pdms.Client.Aad\Pdms.Client.Aad.csproj -p:PackageVersion=%PKG_VERSION% -c %PKG_CONFIG% --no-build --output %REPO_ROOT_DIR%\src\PDMS\nupkgs || exit /b 1
dotnet pack %REPO_ROOT_DIR%\src\PDMS\Product\ClientSdks\ServiceTree\ServiceTree.Client.csproj -p:PackageVersion=%PKG_VERSION% -c %PKG_CONFIG% --no-build --output %REPO_ROOT_DIR%\src\PDMS\nupkgs || exit /b 1

popd
endlocal
echo Everything is awesome! Bye.
exit /b 0