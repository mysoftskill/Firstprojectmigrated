using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy
##############################################################################
#.SYNOPSIS
# Set the AgentReadiness from Test In Prod (TIP) to ProdReady for multiple agents
#
#.DESCRIPTION
# Using the AgentIdsFilePath parameter you can pass a list of agentIds to be moved to ProdReady
#
#.PARAMETER AgentIdsFilePath
# File path of the file that contains all agents that should be set as 'ProdReady'. This file should contain one agentId per line.
#
#.EXAMPLE
# .\Set-AgentReadinessToProdReady.ps1 -AgentIdsFilePath "C:\agentIds.txt" -ErrorAction Stop
##########################################################################################################################################

param(
	[parameter(Mandatory=$true)]
    [string]	
	$AgentIdsFilePath,
    [switch]
    $Force
)

Import-Module PDMS

Connect-PdmsService -Location PROD -ErrorAction Stop

Get-Content $AgentIdsFilePath | ForEach-Object {

    Write-Host "${Get-Date -AsUTC}: Attempting to fetch information with agentId: ${_}"
    
    try
    {
        $agent = Get-PdmsDeleteAgent $_
        if ($agent)
        {
            $prodConnectionDetail = $agent.ConnectionDetails['PROD']

            if ($prodConnectionDetail)
            {
                if ($Force -ne $true)
                {
                    Write-Host "${Get-Date -AsUTC}: Please review the connection details: "
                    $prodConnectionDetail
                    do { $x = Read-Host -Prompt "Press 'y' to apply this change or CTRL+C to quit" } while ($x -ne 'y')
                }
            
                Set-PdmsProperty $prodConnectionDetail AgentReadiness (New-PdmsEnum AgentReadiness 'ProdReady')
                Set-PdmsDeleteAgent $agent
                Write-Host "${Get-Date -AsUTC}: agent id ${_} has been set to ProdReady. Please review the updated connection details: "
                $updatedAgent = Get-PdmsDeleteAgent $_
                $updatedAgent.ConnectionDetails['PROD']
            }
        }
        else
        {
            Write-Host "${Get-Date -AsUTC}: agent id ${_} could not be found."
        }
    }
    catch
    {
        Write-Error "${Get-Date -AsUTC}: An error occured: ${_}"
    }
}

Disconnect-PdmsService