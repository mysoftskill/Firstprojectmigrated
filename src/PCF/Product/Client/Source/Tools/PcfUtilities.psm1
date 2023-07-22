#######################################################
# Helper functions for interacting with the PCF Debug API
#######################################################

function GetEnvironmentConfiguration
{
    param ([string] $EnvironmentType)

    Write-Host "EnvironmentType: $EnvironmentType"

    [hashtable]$return = @{}
    $clientId = "25862df9-0e4d-4fb7-a5c8-dfe8ac261930"
    if ($EnvironmentType -ieq "prod")
    {
        $Scopes = New-Object System.Collections.Generic.List[string]
        $Scopes.Add("api://6bc7725a-1653-4276-914d-fa49341c12ca/debug")

        $return.PcfEndpoint = "pcfv2.privacy.microsoft.com"
        $return.ClientId = $clientId
        $return.Scopes = $Scopes
    }
    elseif ($EnvironmentType -ieq "ppe")
    {
        $Scopes = New-Object System.Collections.Generic.List[string]
        $Scopes.Add("api://3c7702e1-cde9-430b-b3ec-9ae4211c3acb/debug")

        $return.PcfEndpoint = "pcfv2-ppe.privacy.microsoft-ppe.com"
        $return.ClientId = $clientId
        $return.Scopes = $Scopes
    }    
    else
    {
        throw "Unrecognized environment type"
    }

    return $return
}

function GetAuthHeader
{
    param ([hashtable] $EnvironmentConfig)

    $token = Get-MsalToken -Authority "https://login.microsoftonline.com/microsoft.onmicrosoft.com" -RedirectUri "https://management.privacy.microsoft.com" -ClientId $EnvironmentConfig.ClientId -Scopes $EnvironmentConfig.Scopes

    return @{ Authorization="Bearer $($token.AccessToken)" }
}

function CallPcfDebugApi
{
    param (
        [string] $EnvironmentType,
        [string] $Url
    )

    $environmentConfig = GetEnvironmentConfiguration -EnvironmentType $EnvironmentType
    $headers = GetAuthHeader -EnvironmentConfig $environmentConfig
    $finalUrl = "https://$($environmentConfig.PcfEndpoint)/$Url"
    return Invoke-RestMethod -Headers $headers -Uri $finalUrl
}

function Get-CommandPageContent
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId,
        [Parameter(Mandatory=$true)][System.Guid] $AssetGroupId,
        [Parameter(Mandatory=$true)][string] $OperationType,
        [Parameter(Mandatory=$true)][long] $StartTime,
        [Parameter(Mandatory=$true)][long] $EndTime,
        [Parameter(Mandatory=$true)][int] $PageNumber
    )

    $url = "debug/commandpage/content?agentId=$AgentId&assetGroupId=$AssetGroupId&startTimeslice=$StartTime&endTimeslice=$EndTime&operationType=$OperationType&pageNumber=$PageNumber";
    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-CommandPageStats
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId,
        [Parameter(Mandatory=$true)][System.Guid] $AssetGroupId,
        [Parameter(Mandatory=$true)][string] $OperationType,
        [Parameter(Mandatory=$true)][string] $StartTime,
        [Parameter(Mandatory=$true)][string] $EndTime
    )

    $url = "debug/commandpage/stats?agentId=$AgentId&assetGroupId=$AssetGroupId&startTimeslice=$StartTime&endTimeslice=$EndTime&operationType=$OperationType";
    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-CommandPageEntries
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId,
        [Parameter(Mandatory=$true)][System.Guid] $AssetGroupId,
        [Parameter(Mandatory=$true)][string] $OperationType,
        [Parameter(Mandatory=$true)][string] $StartTime,
        [Parameter(Mandatory=$true)][string] $EndTime
    )

    $url = "debug/commandpage/entries?agentId=$AgentId&assetGroupId=$AssetGroupId&startTimeslice=$StartTime&endTimeslice=$EndTime&operationType=$OperationType";
    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-WorkitemStats
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId,
        [System.Guid] $AssetGroupId,
        [int] $Status,
        [string] $StartTime,
        [string] $EndTime
    )

    $url = "debug/workitem/stats?agentId=$AgentId"
    if ($PSBoundParameters.ContainsKey('AssetGroupId'))
    {
        $url += "&assetGroupId=$AssetGroupId"
    }
    if ($PSBoundParameters.ContainsKey('Status'))
    {
        $url += "&status=$Status"
    }
    if ($PSBoundParameters.ContainsKey('StartTime'))
    {
        $url += "&startTimeslice=$StartTime"
    }
    if ($PSBoundParameters.ContainsKey('EndTime'))
    {
        $url += "&endTimeslice=$EndTime"
    }

    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-WorkitemEntries
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId,
        [System.Guid] $AssetGroupId,
        [Parameter(Mandatory=$true)][int] $Status,
        [string] $StartTime,
        [string] $EndTime
    )

    $url = "debug/workitem/entries?agentId=$AgentId&status=$Status"
    if ($PSBoundParameters.ContainsKey('AssetGroupId'))
    {
        $url += "&assetGroupId=$AssetGroupId"
    }
    if ($PSBoundParameters.ContainsKey('StartTime'))
    {
        $url += "&startTimeslice=$StartTime"
    }
    if ($PSBoundParameters.ContainsKey('EndTime'))
    {
        $url += "&endTimeslice=$EndTime"
    }

    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-WorkitemQueueDepth
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId,
        [System.Guid] $AssetGroupId
    )

    $url = "debug/workitem/queuedepth?agentId=$AgentId"
    if ($PSBoundParameters.ContainsKey('AssetGroupId'))
    {
        $url += "&assetGroupId=$AssetGroupId"
    }

    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-DataAgentAssetInfo
{
    param (
        [string] $EnvironmentType=$DefaultEnv
    )

    $url = "debug/dataagentassetinfo"
    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-ExportAcks
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId,
        [System.Guid] $AssetGroupId
    )

    $url = "debug/export/acks?agentId=$AgentId"
    if ($PSBoundParameters.ContainsKey('AssetGroupId'))
    {
        $url += "&assetGroupId=$AssetGroupId"
    }

    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-ExportExpectations
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId,
        [System.Guid] $AssetGroupId,
        [int] $Status
    )

    $url = "debug/export/expectations?agentId=$AgentId"
    if ($PSBoundParameters.ContainsKey('AssetGroupId'))
    {
        $url += "&assetGroupId=$AssetGroupId"
    }
    if ($PSBoundParameters.ContainsKey('Status'))
    {
        $url += "&status=$Status"
    }

    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

function Get-MiscInfo
{
    param (
        [string] $EnvironmentType=$DefaultEnv,
        [Parameter(Mandatory=$true)][System.Guid] $AgentId
    )

    $url = "debug/miscinfo?agentId=$AgentId"
    return CallPcfDebugApi -EnvironmentType $EnvironmentType -Url $url
}

Import-Module MSAL.PS

$DefaultEnv="prod"

Export-ModuleMember -Function Get-CommandPageContent
Export-ModuleMember -Function Get-CommandPageStats
Export-ModuleMember -Function Get-CommandPageEntries
Export-ModuleMember -Function Get-WorkitemStats
Export-ModuleMember -Function Get-WorkitemEntries
Export-ModuleMember -Function Get-WorkitemQueueDepth
Export-ModuleMember -Function Get-DataAgentAssetInfo
Export-ModuleMember -Function Get-ExportAcks
Export-ModuleMember -Function Get-ExportExpectations
Export-ModuleMember -Function Get-MiscInfo
