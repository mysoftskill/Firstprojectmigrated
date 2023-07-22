param (
$Environment="prod",
$AgentId,
$AssetGroupIds,
$StartDate,
$EndDate,
$CsvFile
)

# Summary
# -------
# A wrapper function providing a Cmdlet like interface to submit single replay requests.
function Send-BatchReplayRequest
{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)]
        [string] $AgentId,
        [Parameter(Mandatory=$true)]
        [string] $StartDate,
        [Parameter(Mandatory=$true)]
        [string] $EndDate,
        [string[]] $AssetGroupIds,
        [string] $Environment = "prod"
    )

    Process
    {
       $BatchReplayRequester = [BatchReplayRequester]::new()
       $BatchReplayRequester.SendRequest($AgentId, $StartDate, $EndDate, $AssetGroupIds, $Environment)
    }
}

# Summary
# -------
# A wrapper function providing a Cmdlet like interface to submit replay requests
# through a csv.
function Send-BatchReplayRequestByCSV
{
    [CmdletBinding()]
    param ([string] $CsvFile, [string] $Environment)

    $BatchReplayRequester = [BatchReplayRequester]::new()
    $BatchReplayRequester.SendRequestByCSV($CsvFile, $Environment)
}

# Summary
# -------
# Get's the token given the environment.
function Get-Token
{
    [CmdletBinding()]
    Param(
        [string] $Environment = "prod"
    )

    import-module Microsoft.Identity.Client -RequiredVersion 4.47.2

    $environmentConfig = Get-EnvironmentConfiguration -EnvironmentType $Environment
    $authority = "https://login.microsoftonline.com/microsoft.onmicrosoft.com"
    $redirect = "https://management.privacy.microsoft.com"
    $pcaConfig = [Microsoft.Identity.Client.PublicClientApplicationBuilder]::Create($environmentConfig.ClientId).WithAuthority($authority).WithRedirectUri($redirect) 
    $TokenResult = $pcaConfig.Build().AcquireTokenInteractive($environmentConfig.Scopes).ExecuteAsync().Result; 
    $token = $TokenResult.AccessToken;
    return $token
}

# Summary
# -------
# A helper function that returns all parameters needed to submit a replay request
# for a specific environment.
# 
# Parameters
# ----------
# environment: The enivironment you wish to submit the replay request to (prod/ppe/etc.).
function Get-EnvironmentConfiguration
{
    param ([string] $EnvironmentType)

    [hashtable]$return = @{}
    $clientId = "25862df9-0e4d-4fb7-a5c8-dfe8ac261930"
    if($EnvironmentType -ieq "prod")
    {
        $Scopes = New-Object System.Collections.Generic.List[string]
        $Scopes.Add("api://6bc7725a-1653-4276-914d-fa49341c12ca/replays")

        $return.Uri = "pcfv2.privacy.microsoft.com"
        $return.ClientId = $clientId
        $return.Scopes = $Scopes
    }
    elseif($EnvironmentType -ieq "ppe")
    {
        $Scopes = New-Object System.Collections.Generic.List[string]
        $Scopes.Add("api://3c7702e1-cde9-430b-b3ec-9ae4211c3acb/replays")

        $return.Uri = "pcfv2-ppe.privacy.microsoft-ppe.com"
        $return.ClientId = $clientId
        $return.Scopes = $Scopes
    }    
    else
    {
        throw "Unrecognized environment type"
    }

    return $return
}

# Summary
# -------
# The class responsible for submitting Batch Replay requests.
Class BatchReplayRequester
{
    [hashtable] $tokenCache = @{}

    # Summary
    # -------
    # Get's the token given the environment, cahcing results for future calls.
    [string] GetToken([string] $Environment = "prod")
    {
        if(-not $this.tokenCache.Contains($Environment))
        {
            $this.tokenCache[$Environment] = Get-Token -Environment $Environment
        }
        
        return $this.tokenCache[$Environment]
    }

    # Summary
    # -------
    # Submit's batch replay requests given Agent Id, start date, end date and a list of Asset Group Ids 
    # 
    # Parameters
    # ----------
    # agentId: A string specifying the Agent Id
    # assetGroupIds: A list of strings specifying the Asset Group Ids
    # startDate: The start of the time frame for which to run the replay
    # endDate: The end of the time frame for which to run the replay
    # environment: The environment to run the request on
    [void] SendRequest([string] $AgentId,[string] $StartDate,[string] $EndDate,[string[]] $AssetGroupIds=$null,[string] $Environment = "prod")
    {
        $token= $this.GetToken($Environment)
        $environmentConfig = Get-EnvironmentConfiguration -EnvironmentType $Environment
        $headers = @{ ‘Authorization’ = "Bearer $($token)" } 
        
        $Uri = "https://$($environmentConfig.Uri)/admin/replaycommands" 
        $Body = @{ 
            ”startDate” = "$StartDate" 
            “endDate” = "$EndDate" 
            "agentId" = "$AgentId" 
            “assetGroupIds” = $AssetGroupIds
            "operationType" = "Delete"
            } 
        $Body = ($Body|ConvertTo-Json)
        try 
        {
            Invoke-RestMethod -Uri $Uri -Method Post -Headers $headers -Body $Body -ContentType "application/json" -ErrorVariable RespErr 
        }
        catch [System.Net.WebException] 
        {
            $this.LogResponseToConsole()
            Write-Host "`nSubmission Body:`n$Body `n"
            throw $_.Exception
        }
    }

    # Summary
    # -------
    # Submit's batch replay requests given a csv file and environment. 
    # 
    # Parameters
    # ----------
    # csvFile: The path to the .csv file
    # environment: The enivironment you wish to submit the replay request to.
    # 
    # Notes
    # -----
    # The csv file should contain the folowing headers:
    # AgentId	AssetGroupId	StartDate	EndDate
    [void] SendRequestByCSV([string] $CsvFile, [string] $Environment)
    {
        $sw = [Diagnostics.Stopwatch]::StartNew()

        # replay requests are submitted by (AgentId,StartDate,EndDate), execute sort to facilitate this grouping.
        $replays = Import-Csv -Path $CsvFile | Sort-Object -Property @{Expression={$_.AgentId}}, @{Expression={$_.StartDate}}, @{Expression={$_.EndDate}}, @{Expression={$_.AssetGroupId}}
        $recordToSubmitCount = $replays | Measure-Object | Select-Object -expand count
        $recordSubmittedCount = 0
        $apiCallCount = 0;

        # first invoke requests with empty assetGroupIds as these cannot be grouped together in a single API call
        foreach($replay in $replays)
        {
            if($replay.AssetGroupId -eq '')
            {
                $apiCallCount += 1
                $recordSubmittedCount += 1

                $this.SendRequest($replay.AgentId, $replay.StartDate, $replay.EndDate, $replay.AssetGroupId, $Environment)
                Write-Host "Number of records submitted: $recordSubmittedCount/$recordToSubmitCount"
            }
        }

        # now filter out the entries we have just submitted and continue submission if more exist
        $replays = $replays | Where-Object {$_.AssetGroupId -ne ''}

        if($replays.Length -eq 0)
        {
            return
        }
    
        $currentAgentId   = $replays.First.AgentId
        $currentStartDate = $replays.First.StartDate
        $currentEndDate   = $replays.First.EndDate
        [string[]] $currentAssetGroupIds = @()
        
        foreach($replay in $replays)
        {          
            # Submit replay request and reset if agentId, startDate or endDate changed
            if(($currentAgentId -ne $replay.AgentId) -or ($currentStartDate -ne $replay.StartDate) -or ($currentEndDate -ne $replay.EndDate))
            {
                if ($currentAssetGroupIds.Count -ne 0)
                {
                    $recordSubmittedCount += $currentAssetGroupIds.Count
                    $apiCallCount += 1
                    
                    $this.SendRequest($currentAgentId, $currentStartDate, $currentEndDate, $currentAssetGroupIds, $Environment)
                    Write-Host "Number of records submitted: $recordSubmittedCount/$recordToSubmitCount"
                }

                $currentAssetGroupIds    = @($replay.AssetGroupId)
                $currentStartDate        = $replay.StartDate
                $currentEndDate          = $replay.EndDate
                $currentAgentId          = $replay.AgentId
            }
            else
            {
                $currentAssetGroupIds += $replay.AssetGroupId
            }
        }

        # replay rest of the commands
        if($currentAssetGroupIds.Count -ne 0)
        {
            $recordSubmittedCount += $currentAssetGroupIds.Count
            $apiCallCount += 1

            Write-Host "Number of records submitted: $recordSubmittedCount/$recordToSubmitCount"
            $this.SendRequest($currentAgentId, $currentStartDate, $currentEndDate,$currentAssetGroupIds, $Environment)
        }

        $sw.Stop()

        Write-Host "`nElapsed Time (seconds): $($sw.Elapsed.TotalSeconds) `nNumber of API Calls Made: $apiCallCount"
    }

    # Summary
    # -------
    # A helper functions that performs null checking and logs exceptions that contain a
    # response body.
    [void] LogResponseToConsole()
    {
        if($_.Exception.Response -ne $null)
        {
            $respStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($respStream)
            $respBody = $reader.ReadToEnd()

            Write-Host "`nResponse Body:`n$respBody `n"
        }
    }
}

if($CsvFile -ne $null)
{
    Send-BatchReplayRequestByCSV -CsvFile $CsvFile -Environment $Environment
}
elseif(($AgentId -ne $null) -and ($StartDate -ne $null) -and ($EndDate -ne $null))
{
    Send-BatchReplayRequest -Environment $Environment -AgentId $AgentId -AssetGroupIds $AssetGroupIds -StartDate $StartDate -EndDate $EndDate
}
else
{
    throw "Invalid set of input parameters"
}