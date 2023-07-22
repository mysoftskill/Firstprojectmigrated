using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Identity
##############################################################################
#.SYNOPSIS
# Links a set of asset groups to a set of variant definitions.
# NOTE: This script is deprecated.  Variant registration should be done through DataGrid.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER VariantDefinitionIds
# An array of variant definition ids. A link will be created for each item in this array.
#
#.PARAMETER AssetQualifiersFilePath
# The path to a file that contains a list of asset qualifiers. One entry per line.
#
#.PARAMETER TfsUri
# The TfsUri to use when creating the links.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.PARAMETER DisableSignalFiltering
# Whether or not the variant should have the disable signal filtering flag set. Default is FALSE.
#
#.PARAMETER RefreshCache
# Whether or not the asset group cache should be refreshed. Deafult is FALSE.
#
#.PARAMETER Force
# Whether or not the script should actually make changes. Default is FALSE. This means it runs in a preview mode by default.
#
#.EXAMPLE
# .\New-VariantRegistration.ps1 -VariantDefinitionIds @('3181b272-21db-42e1-9f92-0f690da3236a') -AssetQualifiersFilePath 'C:\Users\petersc\Work Folders\Desktop\variants.txt' -TfsUri 'https://microsoft.visualstudio.com/OSGS/_workitems?id=15408394' -Location PROD -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]
    [string[]]
	$VariantDefinitionIds,
	[parameter(Mandatory=$true)]	
	$AssetQualifiersFilePath,
	[parameter(Mandatory=$true)]
	$TfsUri,
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location,
    [switch]
	$DisableSignalFiltering,
    [switch]
	$RefreshCache,
    [switch]
	$Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

# -- Load the qualifiers from the file and convert into strong typed objects -- #
$assetQualifiers = Get-Content $AssetQualifiersFilePath | ForEach-Object {
    $qualifier = $_ -replace '; ',';' 
    New-PdmsAssetQualifier $qualifier
}

$cache = "$($PSScriptRoot)\VariantOutput\cache.txt"
$updatedResultFile = "$($PSScriptRoot)\VariantOutput\updatedResults.txt"
$failedResultFile = "$($PSScriptRoot)\VariantOutput\failedResults.txt"
$foundResultFile = "$($PSScriptRoot)\VariantOutput\foundResults.txt"
$notFoundResultFile = "$($PSScriptRoot)\VariantOutput\notFoundResults.txt"

$updatedAssetGroups = @()
$failedAssetGroups = @()
$foundAssetQualifiers = @()
$notFoundAssetQualifiers = @()

if ($RefreshCache -or ((Test-Path $cache) -eq $false)) {
    # -- Cache the data because it takes a long time to create -- #
    $assetGroupFilter = New-PdmsObject AssetGroupFilterCriteria
    $assetGroups = Find-PdmsAssetGroups -Filter $assetGroupFilter -Recurse
    ConvertTo-PdmsJson -Value $assetGroups | Out-File $cache
}

# -- Load the data from the cache --#
$json = Get-Content -Raw -Path $cache
$assetGroups = ConvertFrom-PdmsJson -Type 'AssetGroup' -Value $json -Array

# -- Enumerate through the set of asset groups and add variants to those that matter -- #
$assetGroups | ForEach-Object {
	$assetGroup = $_
    $matches = $false
	
    $assetQualifiers | ForEach-Object {
        $result = Invoke-PdmsMethod $_ 'CompareTo' @($assetGroup.Qualifier)
        if ($result -eq 0) {
            $matches = $true
        }
    }

	if ($matches) {
        try {
            $foundAssetQualifiers += $assetGroup.Qualifier
		    $existingVariants = $assetGroup.Variants

            $VariantDefinitionIds | ForEach-Object {
                $VariantDefinitionId = $_

		        # Set the variant link.
		        $variant = New-PdmsObject AssetGroupVariant
		        Set-PdmsProperty $variant 'VariantId' $VariantDefinitionId
		        Set-PdmsProperty $variant 'VariantState' (New-PdmsEnum VariantState 'Approved')
		        Set-PdmsProperty $variant 'DisableSignalFiltering' $DisableSignalFiltering.IsPresent

		        $tfsUris = New-PdmsArray @($TfsUri)
		        Set-PdmsProperty $variant 'TfsTrackingUris' $tfsUris

		        $existingVariants += $variant
            }

		    Set-PdmsProperty $assetGroup 'Variants' (New-PdmsArray $existingVariants)
            
            if ($Force) {
		        $updatedResult = Set-PdmsAssetGroup $assetGroup
            }
            else {
                $updatedResult = $assetGroup
            }

            $updatedResult
            $updatedAssetGroups += $updatedResult
        }
        catch [Exception]{
            Write-Warning $_
            $failedAssetGroups += $assetGroup
        }
	}
}

# -- Identify qualifiers that were not found -- #
$assetQualifiers | ForEach-Object {
    $item = $_
    $matches = $false

    $foundAssetQualifiers | ForEach-Object {
        $result = Invoke-PdmsMethod $_ 'CompareTo' @($item)
        if ($result -eq 0) {
            $matches = $true
        }
    }

    if ($matches -eq $false) {
        $notFoundAssetQualifiers += $item
    }
}

Write-Host '--------------- Updated Asset Groups ---------------------'
New-Item -Force -Path $updatedResultFile -Type file
if ($updatedAssetGroups.Length -gt 0) {
    ConvertTo-PdmsJson -Value $updatedAssetGroups | Out-File $updatedResultFile
}
else {
    '' | Out-File -FilePath $updatedResultFile 
}
$updatedAssetGroups.Count
Write-Host '--------------- Failed Asset Qualifiers ---------------------'
New-Item -Force -Path $failedResultFile -Type file
$failedAssetGroups | ForEach-Object { $_.Qualifier.Value } | Out-File $failedResultFile
$failedAssetGroups.Count
Write-Host '--------------- Found Asset Qualifiers ---------------------'
New-Item -Force -Path $foundResultFile -Type file
$foundAssetQualifiers | ForEach-Object { $_.Value } | Out-File $foundResultFile
$foundAssetQualifiers.Count
Write-Host '--------------- Not Found Asset Qualifiers ---------------------'
New-Item -Force -Path $notFoundResultFile -Type file
$notFoundAssetQualifiers | ForEach-Object { $_.Value } | Out-File $notFoundResultFile
$notFoundAssetQualifiers.Count
Disconnect-PdmsService

if ($Force -eq $false) {
	Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}