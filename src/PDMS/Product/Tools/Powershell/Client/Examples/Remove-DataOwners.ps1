using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Delete a set of data owners.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER InputFile
# The path to a file that contains a list of data owner ids. One entry per line.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.PARAMETER Force
# A switch that enables the execution. Without this the changes run in a preview mode.
#
#.PARAMETER Recurse
# A switch that causes the script to also delete all associated agents and asset groups.
#
#.PARAMETER Force
# Whether or not the script should actually make changes. Default is FALSE. This means it runs in a preview mode by default.
#
#.EXAMPLE
# .\Delete-DataOwners.ps1 -InputFile 'C:\Users\yuhyang\Work Folders\Desktop\dataOwnerIds.txt' -Location PROD -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]	
	$InputFile,
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location,
    [switch]
	$Force,
    [switch]
	$Recurse
)

Import-Module PDMS

Connect-PdmsService -Location $Location

# -- Load the owner ids from the file -- #
$dataOwnerIds = Get-Content $InputFile

Write-Host "Total number of data owner ids to check: $($dataOwnerIds.Length)"
Write-Host "`n"

$deletedOwnerIdFile = "$($PSScriptRoot)\DataOwnerOutput\deletedOwnerIds.txt"
$failedOwnerIdFile = "$($PSScriptRoot)\DataOwnerOutput\failedOwnerIds.txt"
$nonExistingOwnerIdFile = "$($PSScriptRoot)\DataOwnerOutput\nonExistingOwnerIds.txt"
$withWriteSecurityGroupOwnerIdFile = "$($PSScriptRoot)\DataOwnerOutput\withWriteSecurityGroupOwnerIds.txt"

$deletedDataOwnerIds = @()
$failedDataOwnerIds = @()
$nonExistingDataOwnerIds = @()
$withWriteSecurityGroupDataOwnerIds = @()

# -- Enumerate through the set of data owner ids and remove the corresponding data owners -- #
$dataOwnerIds | ForEach-Object {
	$dataOwnerId = $_

	try {
		if ($Recurse) {
			Write-Host "----------------------------------------------------------"

			$f = New-PdmsObject DeleteAgentFilterCriteria
			Set-PdmsProperty $f OwnerId $dataOwnerId
			Find-PdmsDeleteAgents $f | ForEach-Object { 
				$entity = $_ 
				
				Write-Host "Agent: $($entity.Id) valid for deletion"

				if ($Force) {
					Remove-PdmsDeleteAgent -Value $entity
				}
			}

			$f = New-PdmsObject AssetGroupFilterCriteria
			Set-PdmsProperty $f OwnerId $dataOwnerId
			Find-PdmsAssetGroups $f | ForEach-Object { 
				$entity = $_ 
				
				Write-Host "AssetGroup: $($entity.Id) valid for deletion"

				if ($Force) {
					Remove-PdmsAssetGroup -Value $entity
				}
			}
		}

		$dataOwner = Get-PdmsDataOwner -Id $dataOwnerId

		if ($dataOwner -eq $null) {
			# This case will be hit only when the owner does not exist and the user is not running the script in error stop mode. Otherwise, the exception case will catch this.
			Write-Host "$($dataOwnerId) not existing"

			$nonExistingDataOwnerIds += $dataOwnerId
		}
		else {
			Write-Host "DataOwner: $($dataOwnerId) valid for deletion"
		
			if ($Force) {
				Remove-PdmsDataOwner -Value $dataOwner
			}

			$deletedDataOwnerIds += $dataOwnerId
		}
	}
	catch [Exception]{
		Write-Warning $_
		$failedDataOwnerIds += $dataOwnerId
	}
}

Write-Host "`n"

Write-Host '--------------- Deleted Data Owner Ids ---------------------'
New-Item -Force -Path $deletedOwnerIdFile -Type file
$deletedDataOwnerIds | Out-File $deletedOwnerIdFile
$deletedDataOwnerIds.Count

Write-Host "`n"

Write-Host '--------------- Failed Data Owner Ids ---------------------'
New-Item -Force -Path $failedOwnerIdFile -Type file
$failedDataOwnerIds | Out-File $failedOwnerIdFile
$failedDataOwnerIds.Count

Write-Host "`n"

Write-Host '--------------- Non-Existing Data Owner Ids ---------------------'
New-Item -Force -Path $nonExistingOwnerIdFile -Type file
$nonExistingDataOwnerIds | Out-File $nonExistingOwnerIdFile
$nonExistingDataOwnerIds.Count

Write-Host "`n"

Write-Host '--------------- With Write Security Groups Data Owner Ids ---------------------'
New-Item -Force -Path $withWriteSecurityGroupOwnerIdFile -Type file
$withWriteSecurityGroupDataOwnerIds | Out-File $withWriteSecurityGroupOwnerIdFile
$withWriteSecurityGroupDataOwnerIds.Count

Write-Host "`n"

if ($Force -eq $false) {
	Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}