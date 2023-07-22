using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy

#Set all preprod connection details to ProdReady.
Import-Module PDMS

Connect-PdmsService -Location PROD -ErrorAction Stop

$filter = New-PdmsObject DeleteAgentFilterCriteria
Find-PdmsDeleteAgents $filter -Recurse | ForEach-Object {

    $deleteAgent = $_

    if ($deleteAgent)
    {
        $ppeConnectionDetail = $deleteAgent.ConnectionDetails['PreProd']
        if ($ppeConnectionDetail)
        {
            if ($ppeConnectionDetail.AgentReadiness -ne (New-PdmsEnum AgentReadiness 'ProdReady')) {
                Set-PdmsProperty $ppeConnectionDetail AgentReadiness (New-PdmsEnum AgentReadiness 'ProdReady') 
                Set-PdmsDeleteAgent $deleteAgent
            }
        }
    }
}

Disconnect-PdmsService