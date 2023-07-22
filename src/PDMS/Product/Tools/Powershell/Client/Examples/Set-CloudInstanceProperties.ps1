using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy

Import-Module PDMS
Connect-PdmsService -Location "PPE"

# The file path for the csv file with CloudInstance information
$filePath = "\"
$failedResultFile = "$($PSScriptRoot)\CloudInstanceId\failedResults.txt"

$updatedAgents = @()
$failedAgents = @()

Import-Csv $filePath | ForEach-Object {
    $row = $_
    
    try {
        # Forces the statement to catch all exceptions preventing the properties from being set.
        $ErrorActionPreference = "Stop";

        # Set the properties.
        $agent = Get-PdmsDeleteAgent $row.AgentId
        
        Set-PdmsProperty $agent DeploymentLocation (New-PdmsCloudInstance $row.DeploymentLocation)

        Set-PdmsProperty $agent SupportedClouds (New-PdmsCloudInstance $row.SupportedClouds -Array)
        $agent

        # Save the agent.
        Set-PdmsDeleteAgent $agent

        $updatedAgents += $agent.Id
    }
    catch [Exception]{
        Write-Warning $_
        $failedAgents += $agent.Id
    }
}

Write-Host '--------------- Total Agents ---------------------'
$updatedAgents.Count + $failedAgents.Count

Write-Host '--------------- Failed Agents ---------------------'
New-Item -Force -Path $failedResultFile -Type file
$failedAgents | Out-File $failedResultFile
$failedAgents.Count
