using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Finds an agent by the given id and removes the prod connection details.
# If that agent only has prod connection details, then those details
# are move to the PPE state so that the agent always has at least 1 connection detail.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Id
# The agent id to search for.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.EXAMPLE
# .\Remove-ProdConnectionDetails.ps1 -Id '3181b272-21db-42e1-9f92-0f690da3236a' -Location PROD -ErrorAction Stop
##############################################################################
param(
	[parameter(Mandatory=$true)]
	$Id,
	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE','INT')]
	$Location
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$agent = Get-PdmsDeleteAgent -Id $Id

$prod = New-PdmsEnum ReleaseState Prod
$preprod = New-PdmsEnum ReleaseState PreProd

if ((Invoke-PdmsMethod $agent.ConnectionDetails ContainsKey $prod) -eq $true)
{
	if ((Invoke-PdmsMethod $agent.ConnectionDetails ContainsKey $preprod) -eq $false)
	{
		$connectionDetails = $agent.ConnectionDetails
		$releaseState = New-PdmsEnum ReleaseState PreProd
      
		Set-PdmsProperty $connectionDetails Item $agent.ConnectionDetails['Prod'] -Index $releaseState  
	}
  
	$result = Invoke-PdmsMethod $agent.ConnectionDetails Remove $prod
  
	Set-PdmsDeleteAgent -Value $agent
}
else
{
	$agent
}

Disconnect-PdmsService