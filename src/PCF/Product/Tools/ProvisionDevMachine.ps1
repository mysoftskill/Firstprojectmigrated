function GrantNetworkServiceReadAccess
{
    param([String] $fullPath)

    Write-Host "GrantNetworkServiceReadAccess for: $fullPath"

    $rule = new-object security.accesscontrol.filesystemaccessrule "NETWORK SERVICE", read, allow
    if ([io.file]::exists($fullPath))
    {
        $acl = get-acl -path $fullPath
        $acl.addaccessrule($rule)
        set-acl $fullPath $acl
    }
}

function SetCertificatePermissionsForNetworkService
{
    param($cert)

    try
    {
        $keyName  = (($cert.PrivateKey).CspKeyContainerInfo).UniqueKeyContainerName
        $keyPath  = $env:ALLUSERSPROFILE + "\Microsoft\Crypto\RSA\MachineKeys\"
        $fullPath = $keyPath+$keyName
        
        GrantNetworkServiceReadAccess $fullPath
    }
    catch 
    {
        Write-Host "Caught an exception:" -ForegroundColor Red
        Write-Host "$($_.Exception)" -ForegroundColor Red
        exit 1;
    }
}

function ReadAndInstallCertificate
{
    param([String]$pfx, $name)

    $pfxBytes = [Convert]::FromBase64String($pfx)

    [System.IO.File]::WriteAllBytes("c:\tmp-$name.pfx", $pfxBytes)

    try
    {
        Write-Verbose "Using PFX import."
        $cert = Import-PfxCertificate -FilePath "c:\tmp-$name.pfx" Cert:\LocalMachine\My
        SetCertificatePermissionsForNetworkService $cert
        return $cert
    }
    catch
    {
        Write-Host $_
        exit 1;
    }
    finally
    {
        if($name -ne "aad-auth-ppe")
        {
            [System.IO.File]::Delete("c:\tmp-$name.pfx")
        }
    }
}

function UploadSecretToKeyVault
{
    param(
        [string]$keyVaultName, 
        [string]$secretName, 
        [string]$secretValue
        )

    $keyVault = Get-AzKeyVault -VaultName $keyVaultName -ResourceGroupName $resourceGroupName 2> $null
    $softDeleteEnabled = [System.Convert]::ToBoolean($keyVault.EnableSoftDelete)
    $existedSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName 2> $null

    if ($existedSecret -ne $null)
    {
        Write-Host -ForegroundColor Green "Deleting existing secret $secretName";
        Remove-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -Force 2> $null
        
        # Wait for deletion to complete
        do
        {
            Start-Sleep -Milliseconds 100
            $deletedSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -InRemovedState 2> $null
        }
        while ($softDeleteEnabled -eq $true -And $deletedSecret -eq $null)
    }

    if ($softDeleteEnabled -eq $true)
    {
        $deletedSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -InRemovedState 2> $null

        if ($deletedSecret -ne $null)
        {
            Write-Host -ForegroundColor Green "Purging deleted secret $secretName";
            Remove-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -Force -InRemovedState 2> $null
            
            # Wait for purge to complete
            do
            {
                Start-Sleep -Milliseconds 100
                $deletedSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -InRemovedState 2> $null
            }
            while ($deletedSecret -ne $null)
        }
    }

    Write-Host -ForegroundColor Green "Uploading $secretName to Key Vault $keyVaultName";
    $maxRetryCount=10
    $retryCount=0
    do
    {
        Start-Sleep -Milliseconds 100
        $secret = ConvertTo-SecureString -String $secretValue -AsPlainText -Force
        Set-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -SecretValue $secret 2> $null
        $existedSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName 2> $null
        $retryCount = $retryCount + 1
    }
    while ($existedSecret -eq $null -And $retryCount -lt $maxRetryCount)

    if ($retryCount -eq $maxRetryCount)
    {
        Write-Host -ForegroundColor Red "Failed to upload $secretName to Key Vault $keyVaultName after {$maxRetryCount} attempats";
    }
}

function UploadCertToKeyVault
{
 param(
        [string]$keyVaultName, 
        [string]$certName, 
        [string]$filePath
        )

    $keyVault = Get-AzKeyVault -VaultName $keyVaultName -ResourceGroupName $resourceGroupName 2> $null
    $softDeleteEnabled = [System.Convert]::ToBoolean($keyVault.EnableSoftDelete)
    $existedCert = Get-AzKeyVaultCertificate -VaultName $keyVaultName -Name $certName 2> $null
	
    if ($existedCert -ne $null)
    {
        Write-Host -ForegroundColor Yellow "Cert $certName to upload already exists in Key Vault $keyVaultName.";
        do
        {
            $deleteAns = (Read-Host "Delete and upload it again? Enter [y/n]").ToLower()
        }
        while($deleteAns -ne 'y' -and $deleteAns -ne 'n')

        if ($deleteAns -eq 'y')
        {
            Write-Host -ForegroundColor Green "Deleting existing cert $certName from Key Vault $keyVaultName";
            Remove-AzKeyVaultCertificate -VaultName $keyVaultName -Name $certName -Force 
        
            # Wait for deletion to complete
            do
            {
                Start-Sleep -Milliseconds 100
                $deletedCert = Get-AzKeyVaultCertificate -VaultName $keyVaultName -Name $certName -InRemovedState 
            }
            while ($softDeleteEnabled -eq $true -And $deletedCert -eq $null)
        }
        else {
            return
        }
    }

    if ($softDeleteEnabled -eq $true)
    {
        $deletedCert = Get-AzKeyVaultCertificate -VaultName $keyVaultName -Name $certName -InRemovedState 

        if ($deletedCert -ne $null)
        {
            Write-Host -ForegroundColor Green "Purging deleted cert $certName";
            Remove-AzKeyVaultCertificate -VaultName $keyVaultName -Name $certName -Force -InRemovedState 
            
            # Wait for purge to complete
            do
            {
                Start-Sleep -Milliseconds 100
                $deletedCert = Get-AzKeyVaultCertificate -VaultName $keyVaultName -Name $certName -InRemovedState 
                $flag = $deletedCert -ne $null
                Write-Host -ForegroundColor Green "Still purging: $flag";
            }
            while ($deletedCert -ne $null)
        }
    }

    Write-Host -ForegroundColor Green "Uploading $certName to Key Vault $keyVaultName";
    $maxRetryCount=10
    $retryCount=0
    do
    {
        Start-Sleep -Milliseconds 100
		az login
        az keyvault certificate import --file $filePath --name $certName --vault-name $keyVaultName
		$existedCert = Get-AzKeyVaultCertificate -VaultName $keyVaultName -Name $certName 2> $null
        $retryCount = $retryCount + 1
    }
    while ($existedCert -eq $null -And $retryCount -lt $maxRetryCount)

    if ($retryCount -eq $maxRetryCount)
    {
        Write-Host -ForegroundColor Red "Failed to upload $certName to Key Vault $keyVaultName after {$maxRetryCount} attempats";
    }   
    
}

function CreateEventHub
{
    param(
        [string]$resourceGroupName, 
        [string]$eventNamespaceName, 
        [string]$eventHubName
        )
	
    $resource = Get-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventNamespaceName -EventHubName $eventHubName 2> $null
	if ($resource -ne $null)
	{
		Write-Host -ForegroundColor Green "Your Event Hub: $eventHubName already exists"
	}
	else
	{
		Write-Host -ForegroundColor Green "Creating Event Hub: $eventHubName..."
		$resource = New-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventNamespaceName -Name $eventHubName -PartitionCount 2
	}	
}

# Request elevation if this code isn't run as admin
$currentUser = New-Object Security.Principal.WindowsPrincipal $([Security.Principal.WindowsIdentity]::GetCurrent())
$testadmin = $currentUser.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if ($testadmin -eq $false) {
    Start-Process powershell.exe -Verb RunAs -ArgumentList ('-noprofile -noexit -file "{0}" -elevated' -f ($myinvocation.MyCommand.Definition))
    Break
}

# Set working directory to script location
$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
Set-Location $scriptPath

# Check to see if Az module is installed
$azInstallations = Get-InstalledModule -Name Az -AllVersions
if ($azInstallations -eq $null) {
    Write-Host -ForegroundColor Red "Azure Az Powershell module is required to run this script."
    Write-Host -ForegroundColor Red "Please download and install it from https://docs.microsoft.com/en-us/powershell/azure/install-az-ps and run the script from new powershell window"
    exit 1;
}

# Alerting the user to uninstall AzureRM if its already installed
$azrmInstallations = Get-InstalledModule -Name AzureRM -AllVersions 2> $null
if ($azrmInstallations -ne $null -and $PSVersionTable.PSVersion.Major -eq 5) {
	Write-Host -ForegroundColor Red "Please uninstall AzureRM through command 'Uninstall-AzureRm'"
    Write-Host -ForegroundColor Red "Both Az and AzureRM modules were detected on this machine. Az and AzureRM modules cannot be imported in the same session or used in the same script or runbook. If you are running PowerShell in an environment you control you can use the 'Uninstall-AzureRm' cmdlet to remove all AzureRm modules from your machine. If you are running in Azure Automation, take care that none of your runbooks import both Az and AzureRM modules. More information can be found here: https://aka.ms/azps-migration-guide"
    Write-Host -ForegroundColor Yellow "To know more about Az and AzureRM coexistence, please refer to: https://docs.microsoft.com/en-us/powershell/azure/install-az-ps?view=azps-7.3.2#az-and-azurerm-coexistence"
    exit 1;
}

# Check to see if Az is installed
$azInstalled = Get-Command -Name Az 2> $null
if ($azInstalled -eq $null) {
    Write-Host -ForegroundColor Red "Azure CLI is required to run this script."
    Write-Host -ForegroundColor Red "Please download and install it from https://docs.microsoft.com/en-us/powershell/azure/install-az-ps?view=azps-8.0.0 and run the script from new powershell window"
    exit 1;
}

# Login with redmond credentials
# Prompts for login - Use @ame.gbl credentials
Connect-AzAccount -Subscription "ADGCS_R&D"			
if (!$?)
{
    Write-Host "Cannot select ADGCS_R&D subscription. Make sure you joined TM-5881557 at myaccess." -ForegroundColor Red
    exit 1;
}

# Install SSL certificate
$sslPfx = Get-AzKeyVaultSecret -VaultName "NGP-NONPROD-AKV" -Name "pcf-ssl" -AsPlainText
$cloudTestPfx = Get-AzKeyVaultSecret -VaultName "NGP-NONPROD-AKV" -Name "cloudtest-privacy-int" -AsPlainText
$cosmosAdlsPfx = Get-AzKeyVaultSecret -VaultName "NGP-NONPROD-AKV" -Name "aad-auth-ppe" -AsPlainText

Write-Host -ForegroundColor Green "Installing INT certs"
$sslCert = ReadAndInstallCertificate -pfx $sslPfx -name "pcf-ssl-int"
$stsCert = ReadAndInstallCertificate -pfx $cloudTestPfx -name "cloudtest-privacy-int"
$cosmosAdlsCert = ReadAndInstallCertificate -pfx $cosmosAdlsPfx -name "aad-auth-ppe"

# Bind the SSL cert to port 443
$hash = $sslCert.Thumbprint
$output = Start-Process -FilePath netsh -ArgumentList http, delete, sslcert, ipport=0.0.0.0:443 -Wait -Verbose
$output = Start-Process -FilePath netsh -ArgumentList http, add, sslcert, ipport=0.0.0.0:443, "certhash=$hash","appid={00112233-4455-6677-8899-AABBCCDDEEFF}", clientcertnegotiation=disable, verifyclientcertrevocation=disable -Wait -Verbose

Write-Host -ForegroundColor Green "Granting NetworkService to listen https port."
$output = Start-Process -FilePath netsh -ArgumentList http, add, urlacl, "url=https://+:443/", "user=NetworkService" -Wait -Verbose
if (!$?)
{
    Write-Host "Fail to grant listener permissions." -ForegroundColor Red
    exit 1;
}

# Install SSL certificate
$sslPfx = Get-AzKeyVaultSecret -VaultName "NGP-NONPROD-AKV" -Name "pcf-ssl-ppe" -AsPlainText
$stsPfx = Get-AzKeyVaultSecret -VaultName "NGP-NONPROD-AKV" -Name "pcf-sts-ppe" -AsPlainText

Write-Host -ForegroundColor Green "Installing PPE certs"
$sslCert = ReadAndInstallCertificate -pfx $sslPfx -name "pcf-ssl-ppe"
$stsCert = ReadAndInstallCertificate -pfx $stsPfx -name "pcf-sts-onecert-ppe"

# Download nuget.exe if not already exists
if (-not(Test-Path -Path $scriptPath\nuget.exe -PathType Leaf)) {
	Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -Outfile $scriptPath\nuget.exe -UseBasicParsing
}

# Install RPS
## If you encounter the issue below:
#### Product: Windows Live ID Server -- Error 1722. There is a problem with this Windows Installer package. A program run as part of the setup did not finish as expected.
#### Action RegisterRPSSvcServer, location: C:\Program Files\Microsoft Passport RPS\mspprpssvc.exe, command: -regserver
## Ensure that the RPS installation was started in admin mode,
## Ensure that you are installing the x64 version in your 64bit machine.
## Ensure that you have the latest vc++ redistributable (min Visual C++ 2015) For other requirements consult: https://identitydivision.visualstudio.com/IDDP/DevEx-Docs/_git/rps-samples?_a=preview&path=%2FWebApp-WsFedAuth%2FREADME.md&version=GBmaster&createIfNew=false
## If the issue is still present, try to clean up any previous installations and try again.
Write-Host -ForegroundColor Green "Installing RPS"
$rpsNugetPath = "..\..\..\NugetPackages\Microsoft.Passport.Rps.7.1.0\tools"
$rpsExists = Test-Path $rpsNugetPath -PathType Container 
if ($rpsExists -eq $false)
{
    # May prompt for credentials - Use @microsoft.com credentials.
    .\nuget.exe restore packages.config
}

pushd .
cd $rpsNugetPath
Start-Process -FilePath msiexec -ArgumentList /i, rps64.msi, /passive, /norestart -Wait
popd

# Copy rpsserver.xml
Copy-Item ..\Server\CertInstaller\Data\INT\RPS\rpsserver.xml 'C:\Program Files\Microsoft Passport RPS\config' -Force

# GrantNetworkServiceReadAccess to configs
Get-ChildItem "C:\Program Files\Microsoft Passport RPS\config" -Filter *.xml | 
Foreach-Object {
    GrantNetworkServiceReadAccess $_.FullName
}

$resourceGroupName = "NGP_PCF_ONEBOX_RG";

###########
## Provision Azure Key Vault
###########
$keyVaultName = "onebox-$env:username"
$keyVault = Get-AzKeyVault -VaultName $keyVaultName -ResourceGroupName $resourceGroupName 2> $null

if ($keyVault -ne $null)
{
    Write-Host -ForegroundColor Green "Your Key Vault already exists!"
}
else
{
    Write-Host -ForegroundColor Green "Creating you a new Key Vault!"
    $resource = New-AzKeyVault -VaultName $keyVaultName -ResourceGroupName $resourceGroupName -Location "West US 2"
}

# Key vault access policy does not include Purge permission by default
# Must manually update access policies to include 'purge' to handle soft deleted secrets
Write-Host -ForegroundColor Green "Assigning key vault access policies"
$oid = Get-AzADUser -UserPrincipalName (Get-AzContext).Account | select Id
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -ObjectId $oid.Id -PermissionsToSecrets get,list,set,delete,backup,restore,recover,purge -PermissionsToCertificates get,list,delete,create,import,update,managecontacts,getissuers,listissuers,setissuers,deleteissuers,manageissuers,recover,purge -PermissionsToKeys get,create,delete,list,update,import,backup,restore,recover,purge | Out-Null

# Import aad-auth-ppe cert into key vault
$addedAdlsCert = UploadCertToKeyVault -keyVaultName $keyVaultName -certName "aad-auth-ppe" -filePath "C:\tmp-aad-auth-ppe.pfx"

[System.IO.File]::WriteAllText("C:\keyvault.uri.txt", "https://$keyVaultName.vault.azure.net/")

###########
## Provision and upload doc DB keys
###########
$docDbName = "onebox-$env:username"
$resource = Get-AzCosmosDBAccount -ResourceGroupName $resourceGroupName -Name $docDbName  2> $null

if ($resource -ne $null)
{
    Write-Host -ForegroundColor Green "Your doc DB already exists!"
}
else
{
    Write-Host -ForegroundColor Green "Creating you a new doc db!"
    $resource = New-AzCosmosDBAccount -ResourceGroupName $resourceGroupName -Name $docDbName  -Location "West US" -DefaultConsistencyLevel "Strong"
}
[System.IO.File]::WriteAllText("C:\cosmosdb.uri.txt", "https://$docDbName.documents.azure.com:443/")

$keys = Get-AzCosmosDBAccountKey -ResourceGroupName $resourceGroupName -Name $docDbName -Type "Keys"
$addedSecret = UploadSecretToKeyVault -keyVaultName $keyVaultName -secretName "onebox-docdb-key" -secretValue $keys.PrimaryMasterKey.Trim()

$keys = Get-AzCosmosDBAccountKey -ResourceGroupName "NGP_PCF_INT_RG" -Name "pcf-nonprod-pdmscache" -Type "Keys"
$addedSecret = UploadSecretToKeyVault -keyVaultName $keyVaultName -secretName "test-pdmscache-key" -secretValue $keys.PrimaryMasterKey.Trim()

###########
## Provision An upload event hub key.
###########
$eventNamespaceName = "onebox-$env:username";
$resource = Get-AzEventHubNamespace -ResourceGroupName $resourceGroupName -NamespaceName $eventNamespaceName 2> $null
if ($resource -ne $null)
{
    Write-Host -ForegroundColor Green "Your Event Hub Namespace: $eventNamespaceName already exists!";
}
else
{
    Write-Host -ForegroundColor Green "Creating you a new Event Hub Namespace: $eventNamespaceName";
    $resource = New-AzEventHubNamespace -ResourceGroupName $resourceGroupName -NamespaceName $eventNamespaceName -Location "WestUS2" -SkuName Standard -SkuCapacity 1
}

$eventHubName = "commandlifecycle";

CreateEventHub -resourceGroupName $resourceGroupName -eventNamespaceName $eventNamespaceName -eventHubName $eventHubName
CreateEventHub -resourceGroupName $resourceGroupName -eventNamespaceName $eventNamespaceName -eventHubName "altdevicedelete"

$eventConsumerGroups = @("coldstorage","auditlog","rawcommand","telemetry");
foreach($eventConsumerGroup in $eventConsumerGroups)
{
	$resource = Get-AzEventHubConsumerGroup -ResourceGroupName $resourceGroupName -NamespaceName $eventNamespaceName -EventHubName $eventHubName -ConsumerGroupName $eventConsumerGroup 2> $null
	if($resource -ne $null)
	{
		Write-Host -ForegroundColor Green "Your Event Hub Consumer Group: $eventConsumerGroup for Event Hub Name: $eventHubName already exists"
	}
	else
	{
		Write-Host -ForegroundColor Green "Creating Event Hub Consumer Group $eventConsumerGroup for Event Hub Name: $eventHubName..."
		$resource = New-AzEventHubConsumerGroup -ResourceGroupName $resourceGroupName -NamespaceName $eventNamespaceName -EventHubName $eventHubName -ConsumerGroupName $eventConsumerGroup
	}
}

$key = Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $eventNamespaceName -AuthorizationRuleName "RootManageSharedAccessKey"
$addedSecret = UploadSecretToKeyVault -keyVaultName $keyVaultName -secretName "onebox-eventhub-cs" -secretValue $key.PrimaryConnectionString

###########
## Copy Defender API Key into onebox key vault
###########
Write-Host -ForegroundColor Green "Copying the Key for Defender API into onebox key vault..."
$defenderKeyName = "defender-api-key";
$key = Get-AzKeyVaultSecret -VaultName "NGP-NONPROD-AKV" -Name $defenderKeyName -AsPlainText
$addedSecret = UploadSecretToKeyVault -keyVaultName $keyVaultName -secretName $defenderKeyName -secretValue $key

###########
## Grant AAD App permission to Key Vault
###########
Write-Host -ForegroundColor Green "Setting access policies to onebox key vault..."
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -ServicePrincipalName 061be1ab-f7cb-4d44-bc8e-c0dfb357b7fc -PermissionsToSecrets Get,List -PermissionsToCertificates Get,List -PermissionsToKeys Get,List | Out-Null

###########
## Check the installation of Azure Storage Emulator
###########
$emulatorPath = Join-Path -Path ${Env:ProgramFiles(x86)} -ChildPath "Microsoft SDKs\Azure\Storage Emulator"
if (-not (Test-Path $emulatorPath)) {
    Write-Host -ForegroundColor Red "Please go to https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator download and install Azure Storage Emulator."
}
