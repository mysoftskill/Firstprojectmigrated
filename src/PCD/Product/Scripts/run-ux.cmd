@echo off

rem Starts the app's website
::Make sure to have the manage.privacy.microsoft-int.com cert installed
::If you need to run the devbox.cmd script
::navigate to https://dev.manage.privacy.microsoft-int.com

SET ASPNETCORE_ENVIRONMENT=Development
SET ASPNETCORE_URLS=https://dev.manage.privacy.microsoft-int.com:443
SET PCD_EnvironmentName=devbox
pushd "%dp0\..\..\Source\ux"
ux-flattener.cmd >NUL
bin\Debug\ux.exe
popd
