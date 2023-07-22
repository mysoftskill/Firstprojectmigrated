:: Package NuGet packages

setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%

:: stub for cdpx build version. set your version here if running outside the cdpx.
if "%BUILD_BUILDNUMBER%"=="" (
    set BUILD_BUILDNUMBER=0.0.20060.1
)

:: set -pre suffix for non-prod releases.
set PKG_VERSION=%BUILD_BUILDNUMBER%
if not "%~1"=="PROD" (
    set PKG_VERSION=%PKG_VERSION%-pre
)

set PKG_CONFIG=DEBUG
if "%~2"=="" (
    set PKG_CONFIG=DEBUG
) else (
    set PKG_CONFIG=%2
)

dotnet pack %REPO_ROOT_DIR%\src\PXS\Product\PXF\Contracts\PrivacyExperience\Source\PrivacyExperienceContracts.csproj -p:PackageVersion=%PKG_VERSION% -p:Platform=x64 -c %PKG_CONFIG% --no-build --output %REPO_ROOT_DIR%\src\PXS\nupkgs || exit /b 1
dotnet pack %REPO_ROOT_DIR%\src\PXS\Product\PXF\PrivacyExperienceClientLibrary\Source\PrivacyExperienceClientLibrary.csproj -p:PackageVersion=%PKG_VERSION% -p:Platform=x64 -c %PKG_CONFIG% --no-build --output %REPO_ROOT_DIR%\src\PXS\nupkgs || exit /b 1
dotnet pack %REPO_ROOT_DIR%\src\PXS\Product\Contracts\PCFContracts\PXS.Command.Contracts.csproj -p:PackageVersion=%PKG_VERSION% -p:Platform=x64 -c %PKG_CONFIG% --no-build --output %REPO_ROOT_DIR%\src\PXS\nupkgs || exit /b 1

popd
endlocal
echo Everything is awesome! Bye.
exit /b 0