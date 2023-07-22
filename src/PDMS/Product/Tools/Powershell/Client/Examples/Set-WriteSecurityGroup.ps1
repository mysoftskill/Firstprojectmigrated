using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
param(
	[parameter(Mandatory=$true)]
	[string]
	$Id,
	[parameter(Mandatory=$true)]
	[string]
	$SecurityGroupId,
	[string]
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location,
    [switch]
	$Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$team = Get-PdmsDataOwner $Id -Expand ServiceTree
$sg = $team.WriteSecurityGroups + $SecurityGroupId

Set-PdmsProperty $team WriteSecurityGroups (New-PdmsArray $sg)
$team

if ($Force -ne $true) {
    do { $x = Read-Host -Prompt "Press 'Y' to apply this change or CTRL+C to quit" } while ($x -ne 'y')
}

Set-PdmsDataOwner $team