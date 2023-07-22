using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Finds all asset groups that have the given source agent id
# linked based on the given capability. Replaces that agent link
# with a link to the destination agent id. Optionally deletes
# the source agent after moving all asset groups.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Source
# The id of the source agent.
#
#.PARAMETER Destination
# The id of the destination agent.
#
#.PARAMETER Capability
# One of the following values: Delete, Export, *
# Passing * means all capabilities.
#
#.PARAMETER RemoveSource
# An optional switch. When set the source agent id removed.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.EXAMPLE
# Move-AgentIdForAssetGroups -Source 81e6d42e-0eba-42af-9cd8-3271b6760ceb -Destination cb261c86-813c-4ccb-aa23-72a66dfa2749 -Capability * -Location PPE -RemoveSource -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]
	[Guid]
	$Source,
	[parameter(Mandatory=$true)]
	[Guid]
	$Destination,
	[parameter(Mandatory=$true)]
	[ValidateSet("Delete","Export","*")]
	[string]
	$Capability,
	[parameter(Mandatory=$false)]
	[switch]
	$RemoveSource = $false,
	[string]
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location
)

Import-Module PDMS

Connect-PdmsService -Location $Location -ErrorAction Stop

$assetGroupFilter = New-PdmsObject AssetGroupFilterCriteria

$updateDelete = $false
$updateExport = $false

if ($Capability -eq "Delete") {
	Set-PdmsProperty $assetGroupFilter DeleteAgentId $Source	
	$updateDelete = $true
} 
elseif ($Capability -eq "Export") {
	Set-PdmsProperty $assetGroupFilter ExportAgentId $Source
	$updateExport = $true
}
else {
	Set-PdmsProperty $assetGroupFilter DeleteAgentId $Source

	$assetGroupFilterOrClause = New-PdmsObject AssetGroupFilterCriteria
	Set-PdmsProperty $assetGroupFilterOrClause ExportAgentId $Source

	Set-PdmsProperty $assetGroupFilter 'Or' $assetGroupFilterOrClause
	
	$updateDelete = $true
	$updateExport = $true
}

Find-PdmsAssetGroups -Filter $assetGroupFilter | 
ForEach-Object {
	if ($updateDelete -eq $true) {
		Set-PdmsProperty $_ DeleteAgentId $Destination
	}

	if ($updateExport -eq $true) {
		Set-PdmsProperty $_ ExportAgentId $Destination		
	}

	$_
} | 
Set-PdmsAssetGroup

if ($RemoveSource) {
	$Source | Get-PdmsDeleteAgent | Remove-PdmsDeleteAgent
}

Disconnect-PdmsService