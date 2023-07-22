using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Retrieves the 'contact' for a data agent.
# The script outputs who last updated the agent,
# who created the agent, and the linked owner's contact.
# Contacts for an owner are the service admins if linked to service tree,
# or the alert contacts if not linked to service tree.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Id
# The id of the data agent.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.EXAMPLE
# Get-AgentOwnerContacts -Id 81e6d42e-0eba-42af-9cd8-3271b6760ceb -Location PPE -ErrorAction Stop
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
Import-Module PDMSGraph

Connect-PdmsService -Location $Location
Connect-PdmsServiceTree
Connect-PdmsDirectory

$agent = Get-PdmsDeleteAgent -Id $Id -Expand 'TrackingDetails'

$owner = Get-PdmsDataOwner -Id $agent.OwnerId -Expand 'ServiceTree'

if ($owner.ServiceTree -ne $null) {
	$serviceTree = $owner.ServiceTree | Get-PdmsServiceTree
	$contacts = $serviceTree.AdminUserNames
}
else {
	$contacts= $owner.AlertContacts
}

$user = Get-PdmsDirectoryUser -Id $agent.TrackingDetails.UpdatedBy
$updatedBy = $user.Mail
$createdBy = $updatedBy

if ($agent.TrackingDetails.UpdatedBy -ne $agent.TrackingDetails.CreatedBy)
{
	$user = Get-PdmsDirectoryUser -Id $agent.TrackingDetails.CreatedBy
	$createdBy = $user.Mail
}

Write-Host "Agent updated by: {$updatedBy}"
Write-Host "Agent created by: {$createdBy}"
Write-Host "Agent owner contacts: {$contacts}"

Disconnect-PdmsService
Disconnect-PdmsServiceTree
Disconnect-PdmsDirectory