<#
.Synopsis
Remove a VariantDefinition

.Description
Remove a VariantDefinition

.Parameter Location
The environment (PROD, INT, PPE) to operate on

.Parameter VariantId
The VariantDefinition id

.Parameter CloseReason
The reason for closing the VariantDefinition

.Parameter Force
Indicates whether the VariantDefinition should be forcibly deleted
#>

#
#.EXAMPLE
# .\Remove-VariantDefinition.ps1 -Location PROD -VariantId <id> -CloseReason "Expired" -Force
#

param(
    [string]
    [parameter(Mandatory=$true)]
    [ValidateSet('PROD','PPE','INT')]
    $Location,
    [string]
    [parameter(Mandatory=$true)]
    $VariantId,
    [string]
    [parameter(Mandatory=$true)]
    [ValidateSet('None', 'Intentional', 'Expired')]
    $CloseReason,
    [switch]
    $Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

Write-Host "Closing the VariantDefinition with reason $CloseReason"

$variant = Get-PdmsVariantDefinition -Id $VariantId
$variant.Reason = $CloseReason
$variant.State = "Closed"

Set-PdmsVariantDefinition -Value $variant

# Re-get the VariantDefinition since the eTag changed
$variant = Get-PdmsVariantDefinition -Id $VariantId

Write-Host "Deleting the VariantDefinition"

Remove-PdmsVariantDefinition -Value $variant -Force $Force

Disconnect-PdmsService
