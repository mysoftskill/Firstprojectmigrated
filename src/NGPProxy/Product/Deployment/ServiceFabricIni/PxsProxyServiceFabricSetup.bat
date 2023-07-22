ECHO Calling: ServiceFabricSetup.bat
call ServiceFabricSetup.bat

xcopy *.flattened.ini AzureKeyVaultCertificateInstaller /I /Y /F

pushd %~dp0

cd AzureKeyVaultCertificateInstaller
set PXS_EnvironmentName

if "%PXS_EnvironmentName%"=="" (
    echo ERROR: PXS_EnvironmentName is not set.
    exit 1
)

if "%PXS_EnvironmentName%"=="OneBox" (
    ECHO Run Microsoft.PrivacyServices.AzureKeyVaultCertificateInstaller.exe to setup certs in OneBox environment.
) else (
    Microsoft.PrivacyServices.AzureKeyVaultCertificateInstaller.exe
)
cd..

popd

