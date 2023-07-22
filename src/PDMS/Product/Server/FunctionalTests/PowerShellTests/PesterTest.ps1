[CmdletBinding()]
Param(
    [Parameter(
        Mandatory = $true,
        HelpMessage = "Insert the output path for the nunit xml file")]
    [string] $LogFilePath
)

$ErrorActionPreference = "Stop"

$Location = $env:PDMS_TestEnvironmentName
if ($Location -eq $null)
{
    $Location = "INT"
}

Write-Host "Log File Path: $LogFilePath"
Write-Host "Test Environment: $Location"
Write-Host "Current User: $env:USERNAME"

# Save working directory so that we can restore it back after building everything.
$currentPath = Get-Location

Install-PackageProvider -Name "NuGet" -MinimumVersion "2.8.5.201" -Force -Verbose | Out-Null

Set-PSRepository -Name 'PSGallery' -InstallationPolicy Trusted -Verbose | Out-Null

Write-Host "Install Pester, skip publisher check to ignore previous installation and force the installation of the latest module"
Install-Module -Name Pester -RequiredVersion 4.10.1 -Verbose -Force -SkipPublisherCheck

cd $PSScriptRoot

$env:PSModulePath = (Resolve-Path .).Path + ";" + $env:PSModulePath
Import-Module 'PDMS' -Verbose
Import-Module 'PDMSTestHook' -Verbose

$authProvider = New-PdmsAuthenticationProvider
Connect-PdmsService -Location $Location -AuthenticationProvider $authProvider

Write-Host "Invoke Pester to run unit tests in the current folder"
Invoke-Pester . -OutputFile $LogFilePath -OutputFormat NUnitXml

Disconnect-PdmsService

# Restore working directory of user
cd $currentPath
