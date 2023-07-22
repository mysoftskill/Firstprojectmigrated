using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Identity
##############################################################################
#.SYNOPSIS
# Update one or more fields in a Variant Definition.  The variant definition Id
# and Location are required; all other parameters are optional.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Id
# Id of the variant being updated.
#
#.PARAMETER Name
# Updated name of Variant Description.
#
#.PARAMETER Description
# Updated Variant Description.
#
#.PARAMETER EgrcId
# Updated Id of the variant in EGRC
#
#.PARAMETER EgrcName
# Updated Name of the variant in EGRC
#
#.PARAMETER Approver
# Updated Alias of the CELA contact of the variant in EGRC (format: alias@microsoft.com)
#
#.PARAMETER State
# Updated State of the variant 
#
#.PARAMETER Reason
# Updated Reason the variant was closed (Expired or Intentional)
#
#.PARAMETER Capabilities
# Updated array of Capabilities the variant covers.
#
#.PARAMETER SubjectTypes
# Updated array of SubjectTypes the variant covers.
#
#.PARAMETER DataTypes
# Updated array of DataTypes the variant covers.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.PARAMETER Force
# Optional flag indicating whether to commit the changes. The script runs in PREVIEW mode by default.
#
#.EXAMPLE
# .\Update-VariantDefinition.ps1 -Id '1147202f-0da8-4a3e-96a3-d6d1fa041c24' -Name 'DGSS Fraud protection - EXC-1883' -Description 'DGSS Fraud protection' -EgrcId 'EXC-1883' -EgrcName 'DGSS Fraud protection'  -Approver 'syoung@microsoft.com' -Capabilities @('Delete') -SubjectTypes @('AADUser', 'DemographicUser') -DataTypes @('EUII', 'CustomerContent') -State 'Closed' -Reason 'Intentional' -Location PPE -ErrorAction Stop
###################################################################################################################################################################################################################################################################################################################################################
param(
    [parameter(Mandatory=$true)]
	$Id,
    [parameter(Mandatory=$false)]
	$Name,
    [parameter(Mandatory=$false)]
	$Description,
    [parameter(Mandatory=$false)]
	$EgrcId,
    [parameter(Mandatory=$false)]
	$EgrcName,
    [parameter(Mandatory=$false)]
	$Approver,
    [parameter(Mandatory=$false)]
	$State,
    [parameter(Mandatory=$false)]
	$Reason,
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

$variant = Get-PdmsVariantDefinition $Id

Write-Output "Current Variant Definition State:"
$variant

try {
	if ($Name) {
		$variant.Name = $Name
	}

	if ($Description) {
        $variant.Description = $Description
	}
	if ($EgrcId) 
	{
        $variant.EgrcId = $EgrcId
	}
	if ($EgrcName)
	{
	    $variant.EgrcName = $EgrcName
	}
	if ($Approver)
	{
		$variant.Approver = $Approver
	}
	if ($State)
	{
		$variant.State = $State
	}
	if ($Reason)
	{
		$variant.Reason = $Reason
	}

	if ($Capabilities)
	{
		$capabilityList = New-Object System.Collections.Generic.List[Microsoft.PrivacyServices.Policy.CapabilityId]
		$Capabilities | ForEach-Object {
			# set capability.
			$capability = $_
			$newCapability = New-PdmsCapability @($capability)
			$capabilityList.Add($newCapability)
		}
		Set-PdmsProperty $variant Capabilities $capabilityList
	}

	if ($SubjectTypes)
	{
		$subjectTypeList = New-Object System.Collections.Generic.List[Microsoft.PrivacyServices.Policy.SubjectTypeId]
		$SubjectTypes | ForEach-Object {
			# set subjectType.
            $subjectType = $_
            $newSubjectType = New-PdmsSubjectType @($subjectType)
            $subjectTypeList.Add($newSubjectType)
        }
		Set-PdmsProperty $variant SubjectTypes $subjectTypeList
	}

	if ($DataTypes)
	{
		$dataTypeList = New-Object System.Collections.Generic.List[Microsoft.PrivacyServices.Policy.DataTypeId]
		$DataTypes | ForEach-Object {
	        # set dataType.
            $dataType = $_
            $newDataType = New-PdmsDataType @($dataType)
            $dataTypeList.Add($newDataType)
        }
		Set-PdmsProperty $variant DataTypes $dataTypeList
	}

	if ($Force) {
    		$updatedVariant = Set-PdmsVariantDefinition $variant
	}
	else {
	    	$updatedVariant = $variant
	}

	Write-Output "Updated Variant Definition State:"
	$updatedVariant 
}
catch [Exception]{
	Write-Warning $_
}

Disconnect-PdmsService

if ($Force -eq $false) {
	Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}
