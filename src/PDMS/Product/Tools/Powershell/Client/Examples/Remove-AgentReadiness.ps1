using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy

param(
	[parameter(Mandatory=$true)]	
	$AgentIdsFilePath,
    [parameter(Mandatory=$true)]
    $Location
)

Import-Module PDMS

Connect-PdmsService -Location $Location -ErrorAction Stop

Get-Content $AgentIdsFilePath | ForEach-Object {

    $deleteAgent = Get-PdmsDeleteAgent $_

    if ($deleteAgent)
    {
        $prodConnectionDetail = $deleteAgent.ConnectionDetails['PROD']
        if ($prodConnectionDetail)
        {
            Set-PdmsProperty $prodConnectionDetail AgentReadiness (New-PdmsEnum AgentReadiness 'TestInProd')
        }
                
        Set-PdmsDeleteAgent $deleteAgent
    }
}


Disconnect-PdmsService