using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy

##############################################################################
#.SYNOPSIS
# Switch (migrate) an agent Protocol between V1 and V2
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER AgentMigrationState
#  State of Migration: PreproductionV1ToV2, ProductionV1ToV2 or ProductionV2ToV1
#
#.PARAMETER AgentId
# The AgentId, Id of the agent that is migrating
#
#.PARAMETER Protocol
# Protocol to which the agent is moving into (CommandFeedV2 or PCFV2Batch)
#
#.PARAMETER AadAppIds
# A comma separated list of AadAppIds.
#
#.PARAMETER Location
# One of the following values: PPE, PROD
#
#.EXAMPLE
#  1. .\Switch-PdmsDataAgentProtocol.ps1 -AgentMigrationState "PreproductionV1ToV2" -AgentId "B1C4587D-8821-4DA0-B47A-5123993D624E" -Protocol "CommandFeedV2" -AadAppIds @("89EF16C3-F1D3-471C-8B3B-7FB599061111") -Location PPE -ErrorAction Stop
#  2. .\Switch-PdmsDataAgentProtocol.ps1 -AgentMigrationState "ProductionV1ToV2" -AgentId "B1C4587D-8821-4DA0-B47A-5123993D624E" -Protocol "CommandFeedV2" -AadAppIds @("75FEB6C3-F1D3-471C-8B3B-7FB599064ED0", "99FEB6C3-F1D3-471C-8B3B-7FB599067777") -Location PPE -ErrorAction Stop
# Rollback:
#  3. .\Switch-PdmsDataAgentProtocol.ps1 -AgentMigrationState "ProductionV2ToV1" -AgentId "B1C4587D-8821-4DA0-B47A-5123993D624E" -Location PPE -ErrorAction Stop
# Rollforward: 
#  4. .\Switch-PdmsDataAgentProtocol.ps1 -AgentMigrationState "ProductionV1ToV2" -AgentId "B1C4587D-8821-4DA0-B47A-5123993D624E" -Location PPE -ErrorAction Stop
##############################################################################

param(
        [parameter(Mandatory=$true)]
        [string]
        [ValidateSet('PreproductionV1ToV2','ProductionV1ToV2','ProductionV2ToV1')]
        $AgentMigrationState,
        [parameter(Mandatory=$true)]
        [string]
        $AgentId,
        [string]
        [ValidateSet('CommandFeedV2','PCFV2Batch')]
        $Protocol,
        [string[]]
        $AadAppIds,
        [parameter(Mandatory=$true)]
        [string]
        [ValidateSet('PROD','PPE','INT')]
        $Location
)
#Set all preprod connection details to ProdReady.
Import-Module PDMS

Connect-PdmsService -Location $Location -ErrorAction Stop

try
{
    $agentId = [GUID]$AgentId
    
    if ($AgentMigrationState -eq 'PreproductionV1ToV2' -or ($AgentMigrationState -eq 'ProductionV1ToV2' -and $Protocol -and $AadAppIds))
    {
        $connectionDetails = New-PdmsObject ConnectionDetail
        Set-PdmsProperty $connectionDetails Protocol (New-PdmsProtocol $Protocol)
        Set-PdmsProperty $connectionDetails AuthenticationType (New-PdmsEnum AuthenticationType 'AadAppBasedAuth')

        $appIdGuids = @($AadAppIds) | ForEach-Object  { [GUID] $_ }

        Set-PdmsProperty $connectionDetails AadAppIds (New-PdmsArray $appIdGuids)

        if ($AgentMigrationState -eq 'PreproductionV1ToV2')
        {
            Set-PdmsProperty $connectionDetails ReleaseState (New-PdmsEnum ReleaseState 'PreProd')
            Set-PdmsProperty $connectionDetails AgentReadiness (New-PdmsEnum AgentReadiness 'ProdReady') 

            Write-Host '---- Migrating Agent: ' $agentId ' to ' $connectionDetails.Protocol ' ---------' 
            Write-Host '---- Please look at our guidance @ https://aka.ms/BatchvsContinuous on identifying the right PCFV2 protocol to migrate to ---------'
        }
        else
        {
            Set-PdmsProperty $connectionDetails ReleaseState (New-PdmsEnum ReleaseState 'Prod')

            $deleteAgent = Get-PdmsDeleteAgent $agentId
            if ($deleteAgent)
            {   
                Set-PdmsProperty $connectionDetails AgentReadiness $deleteAgent.ConnectionDetails['Prod'].AgentReadiness
            }

            Write-Host '---- Created V2 Prod Connection ---------' $connectionDetails
        }

        do { $x = Read-Host -Prompt "Press 'Y' to switch to V2 Agent or CTRL+C to quit" } while ($x -ne 'y')
        Switch-PdmsDataAgentProtocol (New-PdmsEnum AgentMigrationState $AgentMigrationState) $agentId $connectionDetails
    }
    elseif ($AgentMigrationState -eq 'ProductionV1ToV2')
    {
        do { $x = Read-Host -Prompt "Press 'Y' to switch to V2 Agent or CTRL+C to quit" } while ($x -ne 'y')
        Switch-PdmsDataAgentProtocol (New-PdmsEnum AgentMigrationState $AgentMigrationState) $agentId
    }
    else
    {
        do { $x = Read-Host -Prompt "Press 'Y' to rollback V2 Agent or CTRL+C to quit" } while ($x -ne 'y')

        Switch-PdmsDataAgentProtocol (New-PdmsEnum AgentMigrationState $AgentMigrationState) $agentId 
    }
}
catch [Exception] {
	Write-Warning $_
}

Disconnect-PdmsService