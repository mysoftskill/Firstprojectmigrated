function ReadAndInstallCertificate
{
    param(
        [String]$certName,
        [string]$CertFilePath
    )	
	Write-Host "Installing $certName certificate."
    try
    {
        $cert = Import-PfxCertificate -FilePath "$CertFilePath" Cert:\LocalMachine\My -Password $securePfxPassword 
        return $cert
    }
    catch
    {
        throw "There is some error when installing $certName cert. Make sure it is present in the local."
    }
}

function Install-Certificates([boolean]$setupSsl, [boolean]$setupRps, $localCertFiles) {
    $env:BsstoolPath = $bssToolPath
    $env:CertUsers = "NETWORK SERVICE,$devboxUser"

    $env:RpsRunSetup = "false"
    if ($setupRps) {
        $env:RpsRunSetup = "true"
    }

    $env:SslRunSetup = "false"
    if ($setupSsl) {
        $env:SslRunSetup = "true"
    }
    $env:SslIpAddress = $devboxIpAddress
    $env:SslSetupCorpAccessEndpoint = "false"

    Write-Host "Setting up certificates." -fore Yellow

    #Install SSL and S2S certificates
    Write-Host -ForegroundColor Green "Installing certs"
    if ($localCertFiles[0].contains('ssl'))
    {
        $sslCert = ReadAndInstallCertificate -certName "ssl" -CertFilePath $localCertFiles[0]
        $stsCert = ReadAndInstallCertificate -certName "s2s" -CertFilePath $localCertFiles[1]
    }
    else
    {
        $sslCert = ReadAndInstallCertificate -certName "ssl" -CertFilePath $localCertFiles[1]
        $stsCert = ReadAndInstallCertificate -certName "s2s" -CertFilePath $localCertFiles[0]
    }

    # Bind the SSL cert to port 443
    $hash = $sslCert.Thumbprint
    $output = Start-Process -FilePath netsh -ArgumentList http, delete, sslcert, ipport=0.0.0.0:443 -Wait -Verbose
    $output = Start-Process -FilePath netsh -ArgumentList http, add, sslcert, ipport=0.0.0.0:443, "certhash=$hash","appid={00112233-4455-6677-8899-AABBCCDDEEFF}", clientcertnegotiation=disable, verifyclientcertrevocation=disable -Wait -Verbose   
}
