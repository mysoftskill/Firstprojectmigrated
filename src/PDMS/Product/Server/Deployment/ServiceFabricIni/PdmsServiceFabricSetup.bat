ECHO Calling: ServiceFabricSetup.bat
call ServiceFabricSetup.bat

xcopy *.flattened.ini AzureKeyVaultCertificateInstaller /I /Y /F

pushd %~dp0

cd AzureKeyVaultCertificateInstaller
set PDMS_EnvironmentName

if "%PDMS_EnvironmentName%"=="" (
    echo ERROR: PDMS_EnvironmentName is not set.
    exit 1
)

if "%PDMS_EnvironmentName%"=="devbox" (
    ECHO Run AzureKeyVaultCertificateInstaller.exe to setup certs in devbox environment.
) else (
    AzureKeyVaultCertificateInstaller.exe
)
cd..

popd