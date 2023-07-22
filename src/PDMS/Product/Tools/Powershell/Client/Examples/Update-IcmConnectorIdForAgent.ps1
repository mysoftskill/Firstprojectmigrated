using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Identity
##############################################################################
#.SYNOPSIS
# Update the Icm ConnectorId for the agents listed in CSV.  The file must
# have at least the following 2 columns (additional columns will be ignored):
#
# AgentId,ConnectorId
#
# The script will output two file in .\ConnectorId.   One indicating the agents
# that were updated (updatedAgents.txt) and the other containing the agent ids
# that failed (failedAgents.txt).
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER FilePath
# Path to the CSV input file.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.PARAMETER Force
# Optional flag indicating whether to commit with the changes. The script runs in PREVIEW mode by default.
#
#.EXAMPLE
# Update-IcmConnectorIdForAgent -FilePath .\agentsToUpdate.csv -Location PPE -Force -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]	
	$FilePath,
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location,
    [switch]
	$Force
)

Import-Module PDMS
Connect-PdmsService -Location $Location

$updatedAgents = @()
$failedAgents = @()

$updatedResultFile = "$($PSScriptRoot)\ConnectorId\updatedAgents.txt"
$failedResultFile = "$($PSScriptRoot)\ConnectorId\failedAgents.txt"

Import-Csv $FilePath | ForEach-Object {
    $row = $_
    
    try {
        $agent = Get-PdmsDeleteAgent $row.AgentId 
        $icm = $agent.Icm
        Set-PdmsProperty $icm ConnectorId ([GUID]$row.ConnectorId)
        Set-PdmsProperty $agent Icm $icm
	#Write-Host "$($agent.Id) $($agent.Name) $($icm.ConnectorId)"
    
        if ($Force) {
		    $updatedResult = Set-PdmsDeleteAgent $agent
        }
        else {
            $updatedResult = $agent
        }

        $updatedResult
        $updatedResult.Icm
        $updatedAgents += $agent.Id
    }
    catch [Exception]{
        Write-Warning $_
        $failedAgents += $agent.Id
    }
}

Write-Host '--------------- Total Agents ---------------------'
$updatedAgents.Count + $failedAgents.Count

Write-Host '--------------- Updated Agents ---------------------'
New-Item -Force -Path $updatedResultFile -Type file
$updatedAgents | Out-File $updatedResultFile
$updatedAgents.Count

Write-Host '--------------- Failed Agents ---------------------'
New-Item -Force -Path $failedResultFile -Type file
$failedAgents | Out-File $failedResultFile
$failedAgents.Count
    
if ($Force -eq $false) {
	Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}
