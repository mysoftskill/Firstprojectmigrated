using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy
##############################################################################
#.SYNOPSIS
# Updates an agent's connection details to use PCF protocol
# and corresponding authentication information.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Id
# The id of the data agent.
#
#.PARAMETER MSA
# Set this to use MSA authentication.
#
#.PARAMETER AAD
# Set this to use AAD authentication.
#
#.PARAMETER PPE
# Defines the authentication value to use for PPE details.
# If the MSA flag is set, then this is the MSA Site Id.
# If the  AAD flag is set, then this is the AAD Application Id.
#
#.PARAMETER PROD
# Defines the authentication value to use for PROD details.
# If the MSA flag is set, then this is the MSA Site Id.
# If the  AAD flag is set, then this is the AAD Application Id.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.EXAMPLE
# Set-PcfConnectionDetail -Id 81e6d42e-0eba-42af-9cd8-3271b6760ceb -MSA -PPE 123456 -Location PROD -ErrorAction Stop
##############################################################################
param(
  [parameter(Mandatory=$true)]
  $Id,
  [switch]
  $MSA,  
  [switch]
  $AAD,
  $PPE,
  $PROD,
  [parameter(Mandatory=$true)]
  [ValidateSet('PROD','PPE','INT')]
  $Location
)

Import-Module PDMS

Connect-PdmsService -Location $Location -ErrorAction Stop

$deleteAgent = Get-PdmsDeleteAgent $Id

$protocol = New-PdmsProtocol 'CommandFeedV1';
$prodIndex = New-PdmsEnum ReleaseState Prod
$preprodIndex = New-PdmsEnum ReleaseState PreProd

if ($MSA) {
  if ($PPE) {  
    $value = [System.Int64]::Parse($PPE)
    Write-Host '--------- Original ---------'
    $deleteAgent.ConnectionDetails['PreProd']

	$connectionDetails = New-PdmsObject ConnectionDetail
    Set-PdmsProperty $connectionDetails Protocol $protocol
    Set-PdmsProperty $connectionDetails AuthenticationType (New-PdmsEnum AuthenticationType 'MsaSiteBasedAuth')
    Set-PdmsProperty $connectionDetails MsaSiteId $value
    Set-PdmsProperty $connectionDetails ReleaseState $preprodIndex
    
	# Keep AgentReadiness state
	$readiness = $deleteAgent.ConnectionDetails['PreProd'].AgentReadiness
	Set-PdmsProperty $connectionDetails AgentReadiness $readiness

    Set-PdmsProperty $deleteAgent.ConnectionDetails Item $connectionDetails -Index $preprodIndex
    Write-Host '--------- Updated ---------'
    $deleteAgent.ConnectionDetails['PreProd']
  }
  
  if ($PROD) {
    $value = [System.Int64]::Parse($PROD)
    Write-Host '--------- Original ---------'
    $deleteAgent.ConnectionDetails['Prod']

	$connectionDetails = New-PdmsObject ConnectionDetail
    Set-PdmsProperty $connectionDetails Protocol $protocol
    Set-PdmsProperty $connectionDetails AuthenticationType (New-PdmsEnum AuthenticationType 'MsaSiteBasedAuth')
    Set-PdmsProperty $connectionDetails MsaSiteId $value
    Set-PdmsProperty $connectionDetails ReleaseState $prodIndex
    
	# Keep AgentReadiness state
	$readiness = $deleteAgent.ConnectionDetails['Prod'].AgentReadiness
	Set-PdmsProperty $connectionDetails AgentReadiness $readiness

    Set-PdmsProperty $deleteAgent.ConnectionDetails Item $connectionDetails -Index $prodIndex
    Write-Host '--------- Updated ---------'
    $deleteAgent.ConnectionDetails['Prod']
  }
}

if ($AAD) {
  if ($PPE) {
    $value = [System.Guid]::Parse($PPE)
    Write-Host '--------- Original ---------'
    $deleteAgent.ConnectionDetails['PreProd']

	$connectionDetails = New-PdmsObject ConnectionDetail
    Set-PdmsProperty $connectionDetails Protocol $protocol
    Set-PdmsProperty $connectionDetails AuthenticationType (New-PdmsEnum AuthenticationType 'AadAppBasedAuth')
    Set-PdmsProperty $connectionDetails AadAppId $value
    Set-PdmsProperty $connectionDetails ReleaseState $preprodIndex

	# Keep AgentReadiness state
	$readiness = $deleteAgent.ConnectionDetails['PreProd'].AgentReadiness
	Set-PdmsProperty $connectionDetails AgentReadiness $readiness

    Set-PdmsProperty $deleteAgent.ConnectionDetails Item $connectionDetails -Index $preprodIndex
    Write-Host '--------- Updated ---------'
    $deleteAgent.ConnectionDetails['PreProd']
  }
  
  if ($PROD) {
    $value = [System.Guid]::Parse($PROD)
    Write-Host '--------- Original ---------'
    $deleteAgent.ConnectionDetails['Prod']

	$connectionDetails = New-PdmsObject ConnectionDetail
    Set-PdmsProperty $connectionDetails Protocol $protocol
    Set-PdmsProperty $connectionDetails AuthenticationType (New-PdmsEnum AuthenticationType 'AadAppBasedAuth')
    Set-PdmsProperty $connectionDetails AadAppId $value
    Set-PdmsProperty $connectionDetails ReleaseState $prodIndex

	# Keep AgentReadiness state
	$readiness = $deleteAgent.ConnectionDetails['Prod'].AgentReadiness
	Set-PdmsProperty $connectionDetails AgentReadiness $readiness

    Set-PdmsProperty $deleteAgent.ConnectionDetails Item $connectionDetails -Index $prodIndex
    
    Write-Host '--------- Updated ---------'
    $deleteAgent.ConnectionDetails['Prod']
  }
}


do { $x = Read-Host -Prompt "Press 'Y' to apply this change or CTRL+C to quit" } while ($x -ne 'y')
Set-PdmsDeleteAgent $deleteAgent

Disconnect-PdmsService