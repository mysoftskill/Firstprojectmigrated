using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Finds all asset groups that have the given source owner id
# Replaces that owner link with a link to the destination owner id. 
# Optionally deletes the source owner after moving all asset groups.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Source
# The id of the source owner.
#
#.PARAMETER Destination
# The id of the destination owner.
#
#.PARAMETER RemoveSource
# An optional switch. When set the source owner id removed.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.EXAMPLE
# Move-OwnerIdForAssetGroups -Source 81e6d42e-0eba-42af-9cd8-3271b6760ceb -Destination cb261c86-813c-4ccb-aa23-72a66dfa2749 -RemoveSource -Location PPE -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]
	[string]
	$Source,
	[parameter(Mandatory=$true)]
	[string]
	$Destination,
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
Set-PdmsProperty $assetGroupFilter OwnerId $Source

Find-PdmsAssetGroups -Filter $assetGroupFilter | 
ForEach-Object {
	Set-PdmsProperty $_ OwnerId $Destination
  Set-PdmsAssetGroup $_
}

if ($RemoveSource) {
	$Source | Get-PdmsDataOwner | Remove-PdmsDataOwner
}

Disconnect-PdmsService