using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2

param(
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location,
    [switch]
	$Force
)

Import-Module PDMS
Connect-PdmsService -Location $Location

$aadUser2 = New-PdmsSubjectType @('AADUser2')

function updateVariantDefinitionSubjectType
{
	param([VariantDefinition] $variant)
    Write-Host "Original variant Definition:"
	$variant
	
	try {
		$variant.SubjectTypes.Add($aadUser2)
				
		#confirm update or just preview
		if ($Force) {
			Set-PdmsVariantDefinition $variant
		}
	}
	catch [Exception] {
		Write-Warning $_
	}
	
	Write-Host "Updated Variant Definition:" 
	$variant
}

try {
	$filter = New-PdmsObject VariantDefinitionFilterCriteria
	Find-PdmsVariantDefinitions -Recurse -filter $filter | 
	ForEach-Object {
		$variant = $_
		if ($variant.SubjectTypes -And $variant.State -eq "Active") {	
			
			$needToUpdate = $false
			ForEach ($subjectType in $variant.SubjectTypes) {

				#In current case: we only have AADUser in Prod, but once API changes for update/create VariantDefinition AADUser2 will be live it will make sure that
				#for any create/update on VariantDefinition.SubjectTypes either both AADUser and AADUser2 will exist or none.
				#Considering this fact, this script will check, if AADuser exists and AADUser2 doesn't, it will add AADUser2 to existing SubjectTypes
				#Case 1: Contains only AADUser -> Action=Add AADUser2
				#Case 2: Contains AADuser & AADUser2 -> Action=Do nothing
				#Case 3: Contains AADUser2 & AADUser -> Ordering is different;Assumption is AADUser2 will only be there if AADUser exists already, Action=Do nothing
				#Case 4: Contains only AADUser2 -> Invalid case; Action=Do nothing
				if ([string]$subjectType -eq "AADUser") {
					$needToUpdate = $true
				}
				elseif ([string]$subjectType -eq "AADUser2") {
					$needToUpdate = $false
					#breaking from here, because this case can only happen if the create/update API for VariantDefinition updated SubjectTypes
					#so we do not want to double add AADUser2 in SubjectTypes, hence no need to update
					break
				}
			}

			if ($needToUpdate) {
				updateVariantDefinitionSubjectType -variant $variant
			}
		}
	}
}
catch [Exception] {
	Write-Warning $_
}

Disconnect-PdmsService

if ($Force -eq $false) {
	Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}