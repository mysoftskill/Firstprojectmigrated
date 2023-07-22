using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy

Import-Module PDMS
Connect-PdmsService -Location "PPE"

$failedResultFile = "$($PSScriptRoot)\SovereignCloudId\failedResults.txt"

$updatedAgents = @()
$failedAgents = @()

$filter = New-PdmsObject DeleteAgentFilterCriteria
$filter

Find-PdmsDeleteAgents -Recurse -Filter $filter | 
ForEach-Object {
    try {
        $ErrorActionPreference = "Stop";

        # Set the properties.
        Set-PdmsProperty $_ DeploymentLocation (New-PdmsCloudInstance "Public")
        Set-PdmsProperty $_ SupportedClouds (New-PdmsCloudInstance @("Public") -Array)

        # Save the agent.
        Set-PdmsDeleteAgent $_

        $updatedAgents += $_.Id
    }
    catch [Exception]{
        Write-Warning $_
        $failedAgents += $_.Id
    }
}

Write-Host '--------------- Total Agents ---------------------'
$updatedAgents.Count + $failedAgents.Count

Write-Host '--------------- Failed Agents ---------------------'
New-Item -Force -Path $failedResultFile -Type file
$failedAgents | Out-File $failedResultFile
$failedAgents.Count
