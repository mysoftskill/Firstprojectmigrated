using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2

Import-Module PDMS
Connect-PdmsService PROD

$notFoundAgents = @()
$noConnectorAgents = @()

Import-Csv ".\bad-agents-cosmos.csv" | ForEach-Object { 
    $id = $_.AgentId 
    
    $agent = Get-PdmsDeleteAgent $id

    if ($agent) {
        if ($agent.Icm) {
        }
        else {
            $owner = Get-PdmsDataOwner $agent.OwnerId

            if ($owner.Icm) {

            }
            else {
                $noConnectorAgents += $id
            }
        }
    }
    else {
        $notFoundAgents += $id
    }
}
New-Item -Force -Path ".\Output\Cosmos-NotFoundAgents.txt" -Type file
$notFoundAgents | Out-File ".\Output\Cosmos-NotFoundAgents.txt"

New-Item -Force -Path ".\Output\Cosmos-NoConnectorAgents.txt" -Type file
$noConnectorAgents | Out-File ".\Output\Cosmos-NoConnectorAgents.txt"
