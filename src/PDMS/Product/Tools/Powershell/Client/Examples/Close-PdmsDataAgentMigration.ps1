using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy

##############################################################################
#.SYNOPSIS
# Complete an agent migration between V1 and V2
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER AgentId
# The AgentId, Id of the agent that migrated
#
#.PARAMETER Location
# One of the following values: PPE, PROD
#
#.PARAMETER Force
# Whether or not the script should actually make changes. Default is FALSE. This means it runs in a preview mode by default.
#
#.EXAMPLE
#  .\Close-PdmsDataAgentMigration.ps1 -AgentId "B1C4587D-8821-4DA0-B47A-5123993D624E" -Location PPE -ErrorAction Stop
#
##############################################################################

param(
        [parameter(Mandatory=$true)]
        [string]
        $AgentId,
        [parameter(Mandatory=$true)]
        [string]
        [ValidateSet('PROD','PPE','INT')]
        $Location,
        [switch]
        $Force
)
#Set all preprod connection details to ProdReady.
Import-Module PDMS

Connect-PdmsService -Location $Location -ErrorAction Stop

do { $x = Read-Host -Prompt "Press 'Y' to Permanently remove the migratingConnectionDetails from the agent or CTRL+C to quit" } while ($x -ne 'y')
try
{
    Close-PdmsDataAgentMigration ([GUID]$AgentId)
}
catch [Exception] {
	Write-Warning $_
}


Disconnect-PdmsService

