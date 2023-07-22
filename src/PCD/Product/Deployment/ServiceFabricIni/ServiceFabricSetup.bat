powershell.exe -ExecutionPolicy Bypass -Command ".\ServiceFabricSetup.ps1"
pushd "CertInstaller"
CertInstaller.exe
pushd "%~dp0\..\.."

