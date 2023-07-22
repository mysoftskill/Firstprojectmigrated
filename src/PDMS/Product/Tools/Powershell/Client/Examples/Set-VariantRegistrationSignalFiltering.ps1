using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Identity
##############################################################################
#.SYNOPSIS
# Sets the DisableSignalFiltering flag for an asset group variant registration.
#
#.PARAMETER AssetGroupId
# The Id of the asset group to update.  
#
#.PARAMETER VariantId
# The variant definition id link to update.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.PARAMETER DisableSignalFiltering
# This flag indicates whether or not the variant should have the disable signal filtering flag set. If this flag is not specified,
# DisableSignalFiltering will be set to FALSE.
#
#.PARAMETER Force
# Whether or not the script should actually make changes. Default is FALSE. This means it runs in a preview mode by default.
#
#.EXAMPLE
# .\Set-VariantRegistrationSignalFiltering -AssetGroupId 81e6d42e-0eba-42af-9cd8-3271b6760ceb -VariantId cb261c86-813c-4ccb-aa23-72a66dfa2749 -DisableSignalFiltering -Location PPE -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]
    [string]
	$AssetGroupId,
	[parameter(Mandatory=$true)]
    [string]
	$VariantId,
	[parameter(Mandatory=$true)]
	$Location,
    [switch]
	$DisableSignalFiltering,
    [switch]
	$Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$assetGroup = Get-PdmsAssetGroup $AssetGroupId

try {
    $assetGroup.Variants | ForEach-Object {
        $variant = $_
        
		# Set the DisableSignalFiltering property.
        if ($variant.VariantId -eq $VariantId) {
		    Set-PdmsProperty $variant 'DisableSignalFiltering' $DisableSignalFiltering.IsPresent
        }
        else {
            Write-Warning "Variant id not found."
        }
    }
  
    Write-Host '--------------- AssetGroup ---------------------'
    if ($Force) {
        Set-PdmsAssetGroup $assetGroup
    }
    else {
        $assetGroup
    }
    Write-Host '--------------- AssetGroup.Variants ---------------------'
    $assetGroup.Variants
}
catch [Exception]{
    Write-Warning $_
}
	
if ($Force -eq $false) {
    Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}