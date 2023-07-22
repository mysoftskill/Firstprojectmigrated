@ECHO OFF

ECHO [%DATE% %TIME%] Running Microsoft.Osgs.Infra.CertificateManagement as requested by consumers of feature endpoint "CertificateManagement".

setlocal enabledelayedexpansion

set CertManagerPath=%~dp0
if ""=="!SslCallbackPath!" set SslCallbackPath=%~dp0
if ""=="!CertUsers!" set CertUsers=NETWORK SERVICE

CertInstaller.exe
