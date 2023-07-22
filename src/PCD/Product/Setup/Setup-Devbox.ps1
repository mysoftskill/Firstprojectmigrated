<#

.SYNOPSIS
This script configures devbox environment for PCD.

.PARAMETER setup
Specifies which aspect of devbox environment should be configured. If not
specified, everything will be configured.

Supported values:
* all (default value) - Configures every aspect of devbox environment (needs smart card).
* certs - Installs certificates (needs smart card).
* secrets - Installs files with secrets (needs smart card).
* ssl - Sets up SSL endpoints and installs related certificates (needs smart card).
* iis - Configures IIS Express.
* dependencies - Restores NuGet and NPM dependencies.

.PARAMETER skipDependencies
If provided, indicates whether the NuGet and NPM dependencies should not be
restored, if full devbox configuration is performed.

.EXAMPLE
Setup-Devbox

Setup devbox.

.EXAMPLE
Setup-Devbox -setup ssl

Setup only SSL.

#>

param (
    [string][ValidateSet("all", "certs", "secrets", "ssl", "iis", "dependencies")]$setup = "all",
    [switch]$skipDependencies = $false,
    [switch]$help = $false
)

if ($help) {
    Get-Help $PSCommandPath -Detailed
    exit 0
}

Import-Module .\Modules\utils.psm1

Import-Module .\Modules\nuget.psm1
Import-Module AzureRM # Import the Azure powershell module to talk to key vault.
Import-Module .\Modules\secrets.psm1
Import-Module .\Modules\iisexpress.psm1
Add-Type -AssemblyName System.Windows.Forms

Test-RunningElevated

..\Build\Invoke-Environment "..\Build\buildenv.cmd"
$cwd = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)

$tempFolder = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
mkdir $tempFolder | Out-Null

$FileBrowser = New-Object System.Windows.Forms.OpenFileDialog -Property @{ InitialDirectory = [Environment]::GetFolderPath('Desktop') }
[System.Windows.Forms.Messagebox]::Show("Install the Certificates.The certificates are present in oneDrive. You should be a part of 'NGPIndiaInternal' group to access this file. Choose both downloaded Certificates from local.")
$FileBrowser.Multiselect = $true;
$FileBrowser.ShowDialog()
$localCertFiles = $FileBrowser.FileNames;

try {
    Restore-DevboxNugetDependencies

    $devboxUser = "${env:USERDOMAIN}\${env:USERNAME}"
    $devboxIpAddress = "127.0.0.15"

    $setupAll = $setup -eq "all"

    # Proceed with the rest of the setup.
    if ($setupAll -or $setup -eq "certs" -or $setup -eq "ssl") {
        $setupSsl = $setupAll -or $setup -eq "ssl"
        Install-Certificates -setupSsl $setupSsl -localCertFile $localCertFiles
    }
    if ($setupAll -or $setup -eq "iis") {
        Initialize-IISExpress
    }
    if (($setupAll -and !$skipDependencies) -or $setup -eq "dependencies") {
        Restore-AllDependencies
    }

    Write-Host "`r`nYour devbox environment is ready to use." -fore Green

    Remove-Item -Force -Recurse $tempFolder
    exit 0
}
catch {
    Write-Host $_.Exception.Message -fore Red

    & cmd /c start $tempFolder
    Write-Host "Failed to setup devbox. Please examine output in this window and logs in the temp folder - $tempFolder." -fore Red
    Write-Host "`r`nPlease make sure to follow devbox setup instructions at https://aka.ms/PcdDevbox" -fore Red

    exit 1
}
