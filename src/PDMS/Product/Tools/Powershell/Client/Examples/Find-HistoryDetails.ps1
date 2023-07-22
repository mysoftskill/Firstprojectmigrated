using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
param(
	[parameter(Mandatory=$true)]
	[string]
	$Id,
	[string]
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$filter = New-PdmsObject HistoryItemFilterCriteria
Set-PdmsProperty $filter EntityId $Id
  
Find-PdmsHistoryItems $filter -Recurse |
Sort-Object -Property { $_.Entity.TrackingDetails.Version } |
ForEach-Object {
  Write-Host '-------------------------------'
  $_.Entity
  $_.Entity.TrackingDetails
  $_
}