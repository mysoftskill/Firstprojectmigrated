using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Finds all asset groups that are linked to a specific agent id.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Id
# The variant id to search for.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.EXAMPLE
# .\Find-AssetGroupsByAgentId.ps1 -Id '3181b272-21db-42e1-9f92-0f690da3236a' -Location PROD -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]
	[string]
	$Id,
	[string]
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$filter = New-PdmsObject AssetGroupFilterCriteria
$filter.DeleteAgentId = $Id
$filter.Or = New-PdmsObject AssetGroupFilterCriteria
$filter.Or.ExportAgentId = $Id

Find-PdmsAssetGroups -Filter $filter

Disconnect-PdmsService