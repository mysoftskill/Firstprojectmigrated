using namespace System.Collections.Generic
using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Policy
##############################################################################
#.SYNOPSIS
# Opt-out MsaAgeOut for a specific asset group or a data agent.
#
#.DESCRIPTION
# If asset group ID is provided, we opt-out MsaAgeOut for the specific asset group.
# If agent ID is provided without asset group qualifier, we opt-out all asset
# groups the agent owns. Otherwise we opt-out the asset group with specific qualifier.
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER AssetGroupId
# The asset group id to opt-out.
#
#.PARAMETER AgentId
# The agent id to opt-out.
#
#.PARAMETER AssetGroupQualifier
# The asset group qualifier for the asset group owned by the data agent.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.EXAMPLE
# .\Set-MsaAgeOutOptOut.ps1 -AssetGroupId "c389b038-b385-44dd-b63f-1a5dcd37d65b" -Location "PPE" -ErrorAction Stop
# .\Set-MsaAgeOutOptOut.ps1 -AgentId "aeb3fde9-2b09-4002-bc70-996f8ecc03c9" -Location "PPE" -ErrorAction Stop
# .\Set-MsaAgeOutOptOut.ps1 -AgentId "aeb3fde9-2b09-4002-bc70-996f8ecc03c9" -AssetGroupQualifier "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos11;VirtualCluster=MMDM.test" -Location "PPE" -ErrorAction Stop
##############################################################################
param(
    [parameter(Mandatory=$false)]
    [string]
    $AssetGroupId,
    [parameter(Mandatory=$false)]
    [string]
    $AgentId,
    [parameter(Mandatory=$false)]
    [string]
    $AssetGroupQualifier,
    [string]
    [parameter(Mandatory=$true)]
    [ValidateSet('PROD','PPE','INT')]
    $Location
)

# Function to opt-out MsaAgeOut for the asset group
function OptOutMsaAgeOut($assetGroup)
{  
    if ($assetGroup -ne $null)
    {
        $optionalFeatures = [List[OptionalFeatureId]]($assetGroup.OptionalFeatures)
        if ($optionalFeatures -ne $null -and $optionalFeatures -contains $ageOutFeature)
        {
            $optionalFeatures.Remove($ageOutFeature) | out-null
        }

        Set-PdmsProperty $assetGroup OptionalFeatures $optionalFeatures
        Set-PdmsAssetGroup $assetGroup
    }
} 

if (!$AssetGroupId -and !$AgentId)
{
    Write-Error "Either AssetGroupId or AgentId has to be provided."
}
elseif ($AssetGroupId -and $AgentId)
{
    Write-Error "Both AssetGroupId and AgentId are provided, please only use one of them."
}

Import-Module PDMS
Connect-PdmsService -Location $Location

$ageOutFeature = [Policies]::Current.OptionalFeatures.Ids.MsaAgeOutOptIn

try
{
    if ($AssetGroupId)
    {
        # If AssetGroupId is present, we opt-out the given AssetGroup.
        $assetGroup = Get-PdmsAssetGroup -Id $AssetGroupId
        OptOutMsaAgeOut($assetGroup)
    }
    else
    {
        if ($AssetGroupQualifier)
        {
            $qualifier = New-PdmsAssetQualifier $AssetGroupQualifier
        }

        $filter = New-PdmsObject AssetGroupFilterCriteria
        $filter.DeleteAgentId = $AgentId
        $filter.Or = New-PdmsObject AssetGroupFilterCriteria
        $filter.Or.ExportAgentId = $AgentId

        $assetGroups = Find-PdmsAssetGroups -Filter $filter 
        $assetGroups | ForEach-Object {
            $assetGroup = $_
            if ($AssetGroupQualifier -and ($qualifier -eq $assetGroup.Qualifier))
            {
                OptOutMsaAgeOut($assetGroup)
                exit
            }
            elseif (!$AssetGroupQualifier)
            {
                OptOutMsaAgeOut($assetGroup)
            }
        }

        if ($AssetGroupQualifier)
        {
            Write-Warning "Cannot find the AssetGroup matching the given qualifier $AssetGroupQualifier."
        }
    }
}
catch [Exception]
{
    Write-Warning $_
}
finally
{
    Disconnect-PdmsService
}