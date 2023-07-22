using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Identity
######################################################################################################################
#.SYNOPSIS
# Update the EgrcId and EgrcName for Variants given the VariantId, EgrcId, EgrcName in a CSV file.  
# The file must have at least the following 3 columns (additional columns will be ignored):
#
# VariantId, EgrcId, EgrcName
#
# The script will output two files in .\VariantDefinition.   One indicating the VariantDefinitions
# that were updated (updatedVariantDefinitions.txt) and the other containing the variant ids
# that failed (failedVariantDefinitions.txt).
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
# Optional flag indicating whether to commit the changes. The script runs in PREVIEW mode by default.
#
#.EXAMPLE
# Update-EgrcDetailsForVariantDefinition -FilePath .\variantDefinitionsToUpdate.csv -Location PPE -ErrorAction Stop
##########################################################################################################################
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

$updatedVariantDefinitions = @()
$failedVariantDefinitions = @()

$updatedResultFile = "$($PSScriptRoot)\VariantDefinition\updatedVariantDefinitions.txt"
$failedResultFile = "$($PSScriptRoot)\VariantDefinition\failedVariantDefinitions.txt"

Import-Csv $FilePath | ForEach-Object {
    $row = $_
    
    try {
        $variant = Get-PdmsVariantDefinition  $row.VariantId 
        $variant.EgrcId = $row.EgrcId
        $variant.EgrcName = $row.EgrcName
        # Write-Host "$($variant.Id) ($variant.Name)  $($variant.EgrcId) $($variant.EgrcName)"
    
        if ($Force) {
            $updatedResult = Set-PdmsVariantDefinition $variant
        }
        else {
            $updatedResult = $variant
        }

        $updatedResult
        $updatedVariantDefinitions += $variant.Id
    }
    catch [Exception]{
        Write-Warning $_
        $failedVariantDefinitions += $row.VariantId
    }
}

Write-Host '--------------- Total VariantDefinitions ---------------------'
$updatedVariantDefinitions.Count + $failedVariantDefinitions.Count

Write-Host '--------------- Updated VariantDefinitions ---------------------'
New-Item -Force -Path $updatedResultFile -Type file
$updatedVariantDefinitions | Out-File $updatedResultFile
$updatedVariantDefinitions.Count

Write-Host '--------------- Failed VariantDefinitions ---------------------'
New-Item -Force -Path $failedResultFile -Type file
$failedVariantDefinitions | Out-File $failedResultFile
$failedVariantDefinitions.Count
    
if ($Force -eq $false) {
    Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}
