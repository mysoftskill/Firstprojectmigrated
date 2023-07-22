# How to run Functional Tests locally

## Prerequisite

1. Install nuget package Microsoft.Passport.Rps 7.1.0 for the local RPS server
    - To find this in Visual Studio, search for Passport and right click on the package to select Open in File Explorer
    - Run rps64.msi under the tools folder of the package
2. Install and run [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) (as part of Azure SDK)
3. Make sure you have the sts certificate installed properly on your local machine [Key Vault](https://ms.portal.azure.com/#@MSAzureCloud.onmicrosoft.com/asset/Microsoft_Azure_KeyVault/Certificate/https://pcf-int-ame.vault.azure.net/certificates/pcf-sts-onecert)
    - CN = sts.pcf.privacy.microsoft-int.com, Thumbprint: f9079eb96e84d6901df2964e77bf987a71e1eaec

        (This thumbprint is old; use the most recent cert.  It's also not clear that this step is necessary.)
4. [Generate a self-signed certificate for ssl](#how-to-generate-a-self-signed-ssl-cert)
5. Bind ssl cert from step 4 to port 444 that PartnerMock service will use
    > netsh http add sslcert ipport=0.0.0.0:444 certhash=<your ssl cert thumbprint> appid='{00112233-4455-6677-8899-AABBCCDDEEFF}' clientcertnegotiation=disable verifyclientcertrevocation=disable
6. Load PrivacyExperienceSvc.sln in Visual Studio (in admin mode) and make sure everything are built successfully - this will make sure proper *.flatten.ini are generated for each executable
   
    **Note**: You may need to re-run AzureKeyVaultCertificateInstaller to get the newest MSA cert

## Processes to Launch

1. AzureStorageEmulator.exe
2. Microsoft.Membership.MemberServices.PrivacyMockService.exe - **in admin mode** (either in Visual Studio or from command line depends on your debugging need)
3. Launch one of the following 3 processes depends on what tests you are running (or all 3 of them if you want to run all tests at once)
    * Tests in **AadAccountCloseTests** class: Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.exe - in admin mode (either in Visual Studio or from command line depends on your debugging need)
    * Tests in **ProcessEventsTest** class - Microsoft.Membership.MemberServices.Privacy.AqsWorker - in admin mode (either in Visual Studio or from command line depends on your debugging need) 
    * Everything else: Microsoft.Membership.MemberServices.PrivacyExperience.Service.exe - in admin mode (either in Visual Studio or from command line depends on your debugging need)
4. PrivacyFunctionalTests, preferably from TestExplorer

## Run test(s) from command line

If TestExplorer is too slow for you, use below command to run all tests or selected test from command line:
> "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.dll /InIsolation /Logger:trx /Platform:x64 /TestCaseFilter:"name of the test"

## How to generate a self-signed ssl cert

(openssl.exe can be found in git install location, e.g. "C:\Program Files\Git\usr\bin\openssl.exe")

1. Generate Keys
> openssl.exe req -x509 -nodes -new -sha256 -days 1024 -newkey rsa:4096 -keyout localhost.key -out localhost.pem -subj "/CN=localhost"

2. Generate PFX file
> openssl.exe pkcs12 -export -out localhost.pfx -inkey localhost.key -in localhost.pem

3. Add localhost.pfx to LocalMachine cert store under **both** Personal/Certificates and Trusted Root Certification Authorities/Certificates

4. Note the thumbprint of the cert

## How to update sts cert

After sts.pcf.privacy.microsoft-int.com auto rotated or expired, the new cert need to be registered in MSM:

- [MSM](https://msm.live.com) site registration
    - Choose site - Privacy Experience Service WD, 295750
    - Manage certificates
    - Select the INT site
    - Update the new cert here, purpose needs to be "IDSAPI / Server-Server Authentication"
