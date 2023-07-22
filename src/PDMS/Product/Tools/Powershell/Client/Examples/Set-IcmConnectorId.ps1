using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Identity
##############################################################################
#.SYNOPSIS
# Set the Icm ConnectorId for the owners listed in a CSV input.  The file must
# have at least the following 2 columns (additional columns will be ignored):
#
# OwnerId,ConnectorId
#
# The script will output two file in .\ConnectorId.   One indicating the owners
# that were updated (updatedOwners.txt) and the other containing the owner ids
# that failed (failedOwners.txt).
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
# Set-IcmConnectorId -FilePath .\ownersToUpdate.csv -Location PPE -Force -ErrorAction Stop
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

$updatedOwners = @()
$failedOwners = @()

$updatedResultFile = "$($PSScriptRoot)\ConnectorId\updatedOwners.txt"
$failedResultFile = "$($PSScriptRoot)\ConnectorId\failedOwners.txt"

Import-Csv $FilePath | ForEach-Object {
    $row = $_
    
    try {
        $owner = Get-PdmsDataOwner $row.OwnerId -Expand ServiceTree
        $icm = New-PdmsObject Icm
        Set-PdmsProperty $icm ConnectorId ([GUID]$row.ConnectorId)
        Set-PdmsProperty $owner Icm $icm
		Set-PdmsProperty $icm Source (New-PdmsEnum IcmSource "Manual")
    
        if ($Force) {
		    $updatedResult = Set-PdmsDataOwner $owner        
        }
        else {
            $updatedResult = $owner
        }

        $updatedResult
        $updatedResult.Icm
        $updatedOwners += $owner.Id
    }
    catch [Exception]{
        Write-Warning $_
        $failedOwners += $owner.Id
    }
}

Write-Host '--------------- Total Owners ---------------------'
$updatedOwners.Count + $failedOwners.Count

Write-Host '--------------- Updated Owners ---------------------'
New-Item -Force -Path $updatedResultFile -Type file
$updatedOwners | Out-File $updatedResultFile
$updatedOwners.Count

Write-Host '--------------- Failed Owners ---------------------'
New-Item -Force -Path $failedResultFile -Type file
$failedOwners | Out-File $failedResultFile
$failedOwners.Count
    
if ($Force -eq $false) {
	Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}
