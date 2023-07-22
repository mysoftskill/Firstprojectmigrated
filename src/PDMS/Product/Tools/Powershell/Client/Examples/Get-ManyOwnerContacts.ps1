using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Retrieve the 'contact' information for a list of owners.
#
#.DESCRIPTION
# Outputs the creator, the last editor, and either the service tree admins or the 'alert contacts' alias.
#
#.PARAMETER $InputFile
# The path to a file that contains a list of data owner ids. One entry per line.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.PARAMETER Force
# Whether or not the script should actually make changes. Default is FALSE. This means it runs in a preview mode by default.
#
#.EXAMPLE
# .\Delete-DataOwners.ps1 -DataOwnerIdsFilePath 'C:\Users\yuhyang\Work Folders\Desktop\dataOwnerIds.txt' -Location PROD -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]
	[string]
	$InputFile,
	[string]
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location
)
$startTime = get-date
Write-Host ("Start time: " + $startTime.ToString('T'))
Import-Module PDMS
Import-Module PDMSGraph

Connect-PdmsService -Location $Location
Connect-PdmsServiceTree
Connect-PdmsDirectory

$contactsFile = "$($PSScriptRoot)\EmailOwners\contacts.txt";
$notFoundFile = "$($PSScriptRoot)\EmailOwners\notFound.txt";
$contacts = @{};
$users = @{};
$notFoundIds = @();


function addContacts
{
    param([string] $value, [DataOwner] $owner)
    $id = $owner.Id

    if ($contacts.ContainsKey($id) -ne $true) {
        $contacts[$id] = @{ 'Name' = $owner.Name; 'Contacts' = @(); 'CreatedBy' = $null; 'UpdatedBy' = $null }
    }

    $contacts[$id]['Contacts'] += $value;
}

# load asset groups from file
$ownerIds = Get-Content $InputFile

$processCount = 0
Write-Host ("[" + (get-date).ToString('T') + "]: " + $processCount)

$ownerIds | ForEach-Object {
    # Throttle the script to avoid killing service tree.
    Start-Sleep -m 1000

    $processCount += 1

    if ($processCount % 10 -eq 0) {
        Write-Host ("[" + (get-date).ToString('T') + "]: " + $processCount)
    }

    $ownerId = $_;

    try {
        $owner = Get-PdmsDataOwner -Id $ownerId -Expand 'ServiceTree,TrackingDetails' -ErrorAction Stop

        if ($owner.ServiceTree -ne $null) {
            try {
	            $serviceTree = $owner.ServiceTree | Get-PdmsServiceTree -ErrorAction Stop
	            $serviceTree.AdminUserNames | ForEach-Object { addContacts -value (($_ + "@microsoft.com").ToLower())  -owner  $owner   }
            }
            catch {
                $owner.ServiceTree.ServiceAdmins | ForEach-Object { addContacts -value (($_ + "@microsoft.com").ToLower())  -owner $owner  }
            }
        }
        else {
	        $owner.AlertContacts | ForEach-Object { addContacts -value $_ -owner  $owner  }
        }

        $contacts[$ownerId]['CreatedBy'] = $owner.TrackingDetails.CreatedBy
        $contacts[$ownerId]['UpdatedBy'] = $owner.TrackingDetails.UpdatedBy
        $users[$owner.TrackingDetails.CreatedBy] += 1
        $users[$owner.TrackingDetails.UpdatedBy] += 1
    }
    catch {
        $notFoundIds += $ownerId
    }
}

$users.Keys.Clone() | ForEach-Object {
    $userId = $_;
    if ($userId -ne "EgressJob") {
        try {
            $user = Get-PdmsDirectoryUser -Id $userId
            $users[$userId] = $user.Mail
        }
        catch {
            $users[$userId] = "NotFound [" + $userId + "]"
        }
    }
    else {
        $users[$userId] = "EgressJob"
    }
}


$contacts.Keys | ForEach-Object {
    $contactId = $_;
    $contacts[$contactId]['CreatedBy'] = $users[$contacts[$contactId]['CreatedBy']]
    $contacts[$contactId]['UpdatedBy'] = $users[$contacts[$contactId]['UpdatedBy']]
}

Write-Host '--------------- Contacts ---------------------'
New-Item -Force -Path $contactsFile -Type file
ConvertTo-PdmsJson -Value $contacts | Out-File $contactsFile
$contacts.Keys.Count


Write-Host '--------------- NotFound ---------------------'
New-Item -Force -Path $notFoundFile -Type file
$notFoundIds | Out-File $notFoundFile
$notFoundIds.Count


Write-Host ("Start time: " + $startTime.ToString('T'))
Write-Host ("End time: " + (get-date).ToString('T'))