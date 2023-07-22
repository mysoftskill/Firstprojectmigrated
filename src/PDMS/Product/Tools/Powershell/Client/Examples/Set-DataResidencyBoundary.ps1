using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Set the DataResidencyBoundary value for an agent.  The agent Id and DataResidencyInstance
# are required.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Id
# Id of the agent being updated.
#
#.PARAMETER DataResidencyInstance
# The value to set for DataResidencyBoundary.  Currently valid values are:
# Global and EU.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.PARAMETER Force
# Optional flag indicating whether to commit the changes. The script runs in PREVIEW mode by default.
#
#.EXAMPLE
# .\Set-DataResidencyBoundary.ps1 -Id '1147202f-0da8-4a3e-96a3-d6d1fa041c24' -DataResidencyInstance Global -Location PPE -ErrorAction Stop
##########################################################################################################################################
param(
	[parameter(Mandatory=$true)]
	[string]
	$Id,
	[parameter(Mandatory=$true)]
	[string]
	$DataResidencyInstance,
	[string]
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location,
    [switch]
	$Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$agent = Get-PdmsDeleteAgent $Id

Set-PdmsProperty $agent DataResidencyBoundary (New-PdmsDataResidencyInstance $DataResidencyInstance)

$agent

if ($Force -ne $true) {
    do { $x = Read-Host -Prompt "Press 'Y' to apply this change or CTRL+C to quit" } while ($x -ne 'y')
}

Set-PdmsDeleteAgent $agent