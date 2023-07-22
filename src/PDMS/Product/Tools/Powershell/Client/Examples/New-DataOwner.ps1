using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Create a new Data Owner (team)
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Name
# The Data Owner (team) Name
#
#.PARAMETER Description
# The Data Owner (team) Description
#
#.PARAMETER SecurityGroupIds
# A comma separated group of security Ids that have permission to modify the team configuration.
#
#.PARAMETER AlertContacts
# A comma separated list of emails that can be notified when there are issues with the team's agents or assets.
#
#.PARAMETER Location
# One of the following values: PPE, PROD
#
#.PARAMETER Force
# Whether or not the script should actually make changes. Default is FALSE. This means it runs in a preview mode by default.
#
#.EXAMPLE
# .\New-DataOwner.ps1 -Name TeamName -Description "Team Description" -SecurityGroupIds  @('3181b272-21db-42e1-9f92-0f690da3236a') -AlertContacts @("alias1@microsoft.com", "alias2@microsoft.com" ) -Location PPE -ErrorAction Stop
##############################################################################

param(
        [parameter(Mandatory=$true)]
        [string]
        $Name,
        [parameter(Mandatory=$true)]
        [string]
        $Description,
        [parameter(Mandatory=$true)]
        [string[]]
        $SecurityGroupIds,
        [parameter(Mandatory=$true)]
        [string[]]
        $AlertContacts,
        [parameter(Mandatory=$true)]
        [string]
        [ValidateSet('PROD','PPE','INT')]
        $Location,
        [switch]
        $Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$team = New-PdmsObject -Type DataOwner
$team.Name = $Name
$team.Description = $Description

Set-PdmsProperty $team WriteSecurityGroups (New-PdmsArray $SecurityGroupIds)
Set-PdmsProperty $team AlertContacts (New-PdmsArray $AlertContacts)
$team

if ($Force -ne $true) {
    do { $x = Read-Host -Prompt "Press 'Y' to apply this change or CTRL+C to quit" } while ($x -ne 'y')
}

New-PdmsDataOwner $team
