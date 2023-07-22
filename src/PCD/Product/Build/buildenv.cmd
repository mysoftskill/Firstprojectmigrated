@echo off

pushd "%~dp0"

rem Both "nuget restore" and "dotnet restore" put packages to a well known location
set NugetPackages=%USERPROFILE%\.nuget\packages

rem Package versions used to generate configuration and event classes
set ParallaxPackageVersion=2.28.446.5333
set BondCSharpPackageVersion=11.0.0
set BondCommonSchemaPackageVersion=4.0.21012.1
set OsgsInfraResourcesPackageVersion=10.0.18160.3-beta

if "%VS160COMNTOOLS%"=="" (
    for /f "tokens=*" %%i in ('tools\vswhere.exe -property installationPath -version 16.0 -format value') do set VS160COMNTOOLS=%%i\Common7\Tools
)

rem Add Visual Studio environment variables
call "%VS160COMNTOOLS%\VsMSBuildCmd.bat"

popd
