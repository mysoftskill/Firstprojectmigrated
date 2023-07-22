using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Identity
##############################################################################
#.SYNOPSIS
# Creates a New Variant Definition.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Name
# Name of the variant being created.
#
#.PARAMETER Description
# Variant Description.
#
#.PARAMETER EgrcId
# Id of the variant in EGRC
#
#.PARAMETER EgrcName
# Name of the variant in EGRC
#
#.PARAMETER Approver
# Alias of the CELA contact of the variant in EGRC (format: alias@microsoft.com)
#
#.PARAMETER Capabilities
# An array of Capabilities the variant covers.
#
#.PARAMETER SubjectTypes
# An array of SubjectTypes the variant covers.
#
#.PARAMETER DataTypes
# An array of DataTypes the variant covers.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.PARAMETER Force
# Whether or not the script should actually make changes. Default is FALSE. This means it runs in a preview mode by default.
#
#.EXAMPLE
# .\New-VariantDefinition.ps1 -Name 'DGSS Fraud protection - EXC-1883' -Description 'DGSS Fraud protection' -EgrcId 'EXC-1883' -EgrcName 'DGSS Fraud protection'  -Approver 'syoung@microsoft.com' -Capabilities @('Delete') -SubjectTypes @('AADUser', 'DemographicUser') -DataTypes @('EUII', 'CustomerContent') -Location PPE -ErrorAction Stop
###################################################################################################################################################################################################################################################################################################################################################
param(
    [parameter(Mandatory=$true)]
	$Name,
    [parameter(Mandatory=$true)]
	$Description,
    [parameter(Mandatory=$true)]
	$EgrcId,
    [parameter(Mandatory=$true)]
	$EgrcName,
    [parameter(Mandatory=$true)]
	$Approver,
	[parameter(Mandatory=$false)]
    [string[]]
	$Capabilities,
	[parameter(Mandatory=$false)]
    [string[]]
	$SubjectTypes,
	[parameter(Mandatory=$false)]
    [string[]]
	$DataTypes,
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location,
    [switch]
	$Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$variant = New-PdmsObject VariantDefinition
$variant.Name = $Name
$variant.Description = $Description
$variant.EgrcId = $EgrcId
$variant.EgrcName = $EgrcName
$variant.Approver = $Approver

$capabilityList = New-Object System.Collections.Generic.List[Microsoft.PrivacyServices.Policy.CapabilityId]
$Capabilities | ForEach-Object {
		        # set capability.
                $capability = $_
                $newCapability = New-PdmsCapability @($capability)
                $capabilityList.Add($newCapability)
            }
Set-PdmsProperty $variant Capabilities $capabilityList

$subjectTypeList = New-Object System.Collections.Generic.List[Microsoft.PrivacyServices.Policy.SubjectTypeId]
$SubjectTypes | ForEach-Object {
		        # set subjectType.
                $subjectType = $_
                $newSubjectType = New-PdmsSubjectType @($subjectType)
                $subjectTypeList.Add($newSubjectType)
            }
Set-PdmsProperty $variant SubjectTypes $subjectTypeList

$dataTypeList = New-Object System.Collections.Generic.List[Microsoft.PrivacyServices.Policy.DataTypeId]
$DataTypes | ForEach-Object {
		        # set dataType.
                $dataType = $_
                $newDataType = New-PdmsDataType @($dataType)
                $dataTypeList.Add($newDataType)
            }
Set-PdmsProperty $variant DataTypes $dataTypeList

if ($Force) {
    $newVariant = New-PdmsVariantDefinition -Value $variant
}
else {
    $newVariant = $variant
}

$newVariant
Disconnect-PdmsService

if ($Force -eq $false) {
	Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}