@echo off

Set PCD_EnvironmentName=devbox
echo "Flattening files"
xcopy "%dp0\..\..\..\..\..\Deployment\ServiceFabricIni\IniFlatten.exe" /Y
xcopy "%dp0\..\..\..\..\..\Deployment\ServiceFabricIni\ServiceFabricSetup.ps1" /Y
Powershell.exe -executionPolicy Bypass -Command ".\ServiceFabricSetup.ps1" >NUL
if exist "IniFlatten.exe" (
    del /q "IniFlatten.exe"
    )
if exist "ServiceFabricSetup.ps1" (
    del /q "ServiceFabricSetup.ps1"
    )
Set PCD_EnvironmentName=
