#
#  Replay by given commands and asset group qualifier
#
function SelfServeReplayByCommandIds
{
    param ([string] $agentId, [string] $assetGroupQualifier, [string[]] $commandIds, [string] $environment)
    Write-Output  "Request: $agentId,$assetGroupQualifier,$($commandIds.Count),$($commandIds[0]),$environment"

    $sw = [Diagnostics.Stopwatch]::StartNew()

    $assetGroupQualifiers = @($assetGroupQualifier)
    $replayRequest = @{assetQualifiers = $assetGroupQualifiers; commandIds = $commandIds}
    $requestBody = $replayRequest | ConvertTo-Json

    $response = InvokeS2SWebRequest "debug/replaycommands/$agentId" $environment $true $requestBody

    if ($response.StatusCode -eq 200)
    {
        $sw.Stop()
        Write-Output "Elapsed: $($sw.Elapsed.ToString())"

        return "SelfServeReplay requested"



    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    $sw.Stop()
    Write-Output "Elapsed: $($sw.Elapsed.ToString())"

    return $parsedResponse
}

#
#   Request PCF SelfServeReplay from csv file that contains list of the asset group qualifiers and commands .
#   The csv file should include headers:
#   AgentId,AssetGroupId,AssetQualifier,CommandId,TimeStamp
#   3fad1946-76c0-4a65-8445-38d012bb3f3c,19dec147-62c9-4534-87fb-5f83567b001e,AssetType=SqlServer;ServerName=episql.343i.selfhost.corp.microsoft.com;DatabaseName=TicketTrack_Shiva,4e610867-9a57-4dc1-9674-7b7d8898b781,2020-03-11 23:59:19.6167705
#   c4a20565-4def-4b14-9771-8fe1a61404e8,97786d01-6f85-4d7b-923d-04dc7e14dbc7,AssetType=SqlServer;ServerName=episql.343i.selfhost.corp.microsoft.com;DatabaseName=TicketTrack_Osiris,4e610867-9a57-4dc1-9674-7b7d8898b781,2020-03-11 23:59:19.4556904
#   
#   Note: the input file should be sorted by AgentId,AssetGroupId,AssetQualifier
# 
#  Request-SelfServeReplayCommandsFromCsv -csvFile c:\MYFILE.CSV -environment PPE
#  
#  Returns HTTP request error.
#
function Request-SelfServeReplayCommandsFromCsv
{
    param ([string] $csvFile, [string] $environment)

    $startTime = Get-Date -format s
    Write-Output "Request: $csvFile $environment. Start time: $startTime."

    $replays = Import-Csv -Path $csvFile
    $totalRecords = $replays | Measure-Object | Select-Object -expand count
    $totalReplayed = 0
    $totalCount = 0


    $sw = [Diagnostics.Stopwatch]::StartNew()
    $currentAgq = "xxx"
    $currentAgentId = "xxx"
    [string[]] $commandIds = @()

    foreach ($replay in $replays)
    {
        $totalCount += 1
        # Reset and replay if Agent or AG has changed
        if (($currentAgq -ne $replay.AssetQualifier) -or ($currentAgentId -ne $replay.AgentId))
        {
            if ($commandIds.Count -ne 0)
            {
                Write-Output "Sending: $($commandIds.Count). Replayed: $totalReplayed/$totalRecords"
                SelfServeReplayByCommandIds -agentId $currentAgentId -assetGroupQualifier $currentAgq -commandIds $commandIds -environment $environment
                $totalReplayed += $commandIds.Count
            }

            $commandIds = @($replay.CommandId)
            $currentAgq = $replay.AssetQualifier
            $currentAgentId = $replay.AgentId
        }
        elseif ($commandIds.Count -eq 50)
        {
            Write-Output "Sending: $($commandIds.Count). Replayed: $totalReplayed/$totalRecords"
            SelfServeReplayByCommandIds -agentId $currentAgentId -assetGroupQualifier $currentAgq -commandIds $commandIds -environment $environment
            $totalReplayed += $commandIds.Count
            $commandIds = @($replay.CommandId)
        }
        else
        {
            $commandIds += $replay.CommandId
        }
    }

    # replay rest of the commands
    if ($commandIds.Count -ne 0)
    {
        Write-Output "Sending: $($commandIds.Count). Replayed: $totalReplayed/$totalRecords"
        SelfServeReplayByCommandIds -agentId $replay.AgentId -assetGroupQualifier $currentAgq -commandIds $commandIds -environment $environment
        $totalReplayed += $commandIds.Count
        [string[]] $commandIds = @()
    }

    $sw.Stop()
    $endTime = Get-Date -format s

    Write-Output "Start time: $startTime."
    Write-Output "End time: $endTime."
    Write-Output "Elapsed: $($sw.Elapsed.ToString()). Replayed: $totalReplayed/$totalRecords/$totalCount."
}

#
#  Request PCF SelfServeReplay from given list of the agent id, asset group qualifier, dates and includeExportCommands in csv file.
#  The csv file should include headers:
#   AgentId,AssetGroupQualifier,StartDate,EndDate,IncludeExportCommands
#   3fad1946-76c0-4a65-8445-38d012bb3f3c,AssetType=SqlServer;ServerName=episql.343i.selfhost.corp.microsoft.com;DatabaseName=TicketTrack_Shiva,08/24/19,08/24/19,$true
#   c4a20565-4def-4b14-9771-8fe1a61404e8,AssetType=SqlServer;ServerName=episql.343i.selfhost.corp.microsoft.com;DatabaseName=TicketTrack_Osiris,08/24/19,08/24/19,$false
# 
#  Request-SelfServeReplayByAssetGroupQualifierFromCsv -csvFile c:\MYFILE.CSV -environment PPE
#  
#  Returns HTTP error.
#
function Request-SelfServeReplayByAssetGroupQualifierFromCsv
{
    param ([string] $csvFile, [string] $environment)

    $replays = Import-Csv -Path $csvFile
    foreach ($replay in $replays)
    {
        Write-Host $replay.AgentId $replay.AssetGroupQualifier, $replay.StartDate $replay.EndDate $replay.IncludeExportCommands
        SelfServeReplayByAssetGroupQualifier -agentId $replay.AgentId -assetGroupQualifier $replay.AssetGroupQualifier -replayFromDate $replay.StartDate -replayToDate $replay.EndDate -includeExportCommands $replay.IncludeExportCommands -environment $environment
    }
}

#
#  Request PCF SelfServeReplay from given list of the agents and dates in csv file.
#  The csv file should include headers:
# AgentId,StartDate,EndDate,IncludeExportCommands
# d8c8d1f8-3241-480e-a0b5-256a2fa32216,08/24/19,08/24/19,$true
# 423847cd-0d8f-4f5b-bf91-08c75f521a1a,08/24/19,08/24/19,$true
# 713ca2c1-089a-4f72-8b3e-134444d7f84f,08/24/19,08/24/19,$false
#     
# 
#  Request-SelfServeReplayFromCsv -csvFile c:\MYFILE.CSV -environment PPE
#  
#  Returns HTTP error.
#
function Request-SelfServeReplayFromCsv
{
    param ([string] $csvFile, [string] $environment)

    $replays = Import-Csv -Path $csvFile
    foreach ($replay in $replays)
    {
        Write-Host $replay.AgentId $replay.StartDate $replay.EndDate $replay.IncludeExportCommands
        SelfServeReplay -agentId $replay.AgentId -replayFromDate $replay.StartDate -replayToDate $replay.EndDate -includeExportCommands $replay.IncludeExportCommands -environment $environment
    }
}

#
#  Force complete export command.
#     Complete-ForceCompleteCommand -commandId 64e0ebb82dd84c1ea707864566b7e2d9 -environment PPE
#  
#  Returns HTTP error.
#
function Complete-ForceCompleteCommand
{
    param ([string] $commandId, [string] $environment)
    
    $response = InvokeS2SWebRequest "debug/completecommand/$commandId" $environment

    if ($response.StatusCode -eq 204)
    {
        Write-Host "Command ID not found"
        return $response.StatusCode
    }

    $parsedResponse = "StatusCode: " + $response.StatusCode + " Content: " + $response.Content
    Write-Host $parsedResponse

    return $response.StatusCode
}

#
#  Fetches status for a given command ID.
#     Get-CommandStatus -commandId 64e0ebb82dd84c1ea707864566b7e2d9 -environment PPE
#  
#  Returns an object representing the JSON from the response.
#
function Get-CommandStatus
{
    param ([string] $commandId, [string] $environment)
    
    $response = InvokeS2SWebRequest "debug/status/commandid/$commandId" $environment

    if ($response.StatusCode -eq 204)
    {
        Write-Host "Command ID not found"
        return
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    return $parsedResponse
}

function ResetNextVisibleTime
{
    param ([string] $agentId, [string] $assetGroupId, [string] $commandId, [string] $environment)
    
    $response = InvokeS2SWebRequest "debug/resetnextvisibletime/$agentId/$assetGroupId/$commandId" $environment

    Write-Host "Response code: " $response.StatusCode
    
    if ($response.StatusCode -eq 200)
    {
        Write-Host "Command ID: $commandId next visible time successfully reset for Agent id: $agentId. Asset Group id: $assetGroupId."
        return
    }

    return $response.Content
}

#
#  Fetches PDMS data, encoded as a JSON response. This function has two optional parameters: agentId and data set version.
#  If not specified, all PDMS data from the latest version will be returned.
# 
#  Otherwise, data matching the specific agent and/or version will be returned so that the result set is smaller.
#  
#  Get-PdmsData -agentId 64e0ebb82dd84c1ea707864566b7e2d9 -dataSetVersion 3 -environment PPE
#  Get-PdmsData -dataSetVersion 2 -environment PPE
#  Get-PdmsData -agentId 64e0ebb82dd84c1ea707864566b7e2d9 -environment PPE
#  Get-PdmsData -environment PPE
#
function Get-PdmsData
{
    param ([string] $agentId = "", [string] $dataSetVerison = "", [string] $environment)
    
    $queryString = ""

    if ($agentId -ne "")
    {
        $queryString += "agent=$agentId"
    }

    
    if ($dataSetVerison -ne "")
    {
        $queryString += "&version=$dataSetVerison"
    }

    $response = InvokeS2SWebRequest "debug/dataagentmap?$queryString" $environment

    $parsedResponse = ConvertFrom-Json $response.Content
    return $parsedResponse
}

#
#  Fetches queue statistics for a given agent ID in the given environment.
#
#  Get-QueueStats -agentId 64e0ebb82dd84c1ea707864566b7e2d9 -environment PPE
#
function Get-QueueStats
{
    param ([string] $agentId, [string] $environment, [bool] $detailed = $true)

    $response = InvokeS2SWebRequest "debug/queuestats/$agentId/?detailed=$detailed" $environment

    $parsedResponse = ConvertFrom-Json $response.Content
    return $parsedResponse
}

#  NO LONGER IN USE
#  This command was used before PCF went live in May 2018, and has not been used since. 
#  In order to maintain stability of the service, it is not recommended to use this command
#
#  Flushes the agent queue for a given agent ID in the given environment.
#
#  FlushAgentQueue -agentId 64e0ebb82dd84c1ea707864566b7e2d9 -flushDate 20180421 -environment PPE
#
function FlushAgentQueue
{
    param ([string] $agentId = "", [string] $flushDate = "", [string] $environment)
    
    $queryString = ""

    if ($agentId -ne "")
    {
        $queryString += "agent=$agentId"
    }

    if ($flushDate -ne "")
    {
        $queryString += "&flushDate=$flushDate"
    }

    $response = InvokeS2SWebRequest "debug/flushqueue?$queryString" $environment

    if ($response.StatusCode -eq 200)
    {
        return "AgentQueueFlush requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

#
#  Run ingestion recovery pipeline
#
#  RunIngestionRecovery -startDate 20230304 -endDate 20230309 -exportOnly true -nonExportOnly false -environment PPE
#
function RunIngestionRecovery
{
    param ([string] $startDate = "", [string] $endDate = "", [string] $exportOnly = "", $nonExportOnly = "true", [string] $environment)   

    $response = InvokeS2SWebRequest "debug/ingestionrecovery/$startDate/$endDate/$exportOnly/$nonExportOnly" $environment $true

    if ($response.StatusCode -eq 200)
    {
        return "Ingestion Recovery requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

#
#  Replay selected previous days' of commands for every agents
#
#  ReplayForAll -replayFromDate 20180421 -replayToDate 20180429 -environment PPE
#
function ReplayForAll
{
    param ([string] $replayFromDate = "", [string] $replayToDate = "", [string] $environment)
    
    $queryString = ""

    if ($replayFromDate -ne "")
    {
        $queryString += "replayFromDate=$replayFromDate"
    }

    if ($replayToDate -ne "")
    {
        $queryString += "&replayToDate=$replayToDate"
    }

    $response = InvokeS2SWebRequest "debug/replayforall?$queryString" $environment

    if ($response.StatusCode -eq 200)
    {
        return "Replay-For-All requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

#
#  Replay selected previous days' of commands for one single agent excluding export commands (-includeExportCommands $true to include all export commands)
#
#  SelfServeReplay -agentId XXXXXXX -replayFromDate "2018-06-29" -replayToDate "2018-06-29" -includeExportCommands $false -environment PPE
#
function SelfServeReplay
{
    param ([string] $agentId = "", [string] $replayFromDate = "", [string] $replayToDate = "", [bool] $includeExportCommands = $false, [string] $environment)
    
    $replayRequest = @{replayFromDate = $replayFromDate; replayToDate = $replayToDate; includeExportCommands = $includeExportCommands}
    $requestBody = $replayRequest | ConvertTo-Json

    $response = InvokeS2SWebRequest "debug/replaycommands/$agentId" $environment $true $requestBody

    if ($response.StatusCode -eq 200)
    {
        return "SelfServeReplay requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

#
#  Replay selected command for agentId
#
#  SelfServeReplayById
#      -agentId 848f89f9f2f04317acef00fe1cf1e12e
#      -assetGroupQualifier "AssetType=ApplicationService;Host=11960d24-92fa-4ca5-9470-2a6e332141fe;Path=Consumer-59608ee1-4cd7-11e8-bacc-c955079f5b12"
#      -commandId 93c6ccb9505147ee8e9542b11277c18d
#      -environment PROD
#
function SelfServeReplayById
{
    param ([string] $agentId, [string] $assetGroupQualifier, [string] $commandId, [string] $environment)
    
    $commandIds = @($commandId)
    $assetGroupQualifiers = @($assetGroupQualifier)
    $replayRequest = @{assetQualifiers = $assetGroupQualifiers; commandIds = $commandIds}
    $requestBody = $replayRequest | ConvertTo-Json

    $response = InvokeS2SWebRequest "debug/replaycommands/$agentId" $environment $true $requestBody

    if ($response.StatusCode -eq 200)
    {
        return "SelfServeReplay requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

#
#  Replay commands for a subjectType for a specified date range excluding export Commands
#
#  SelfServeReplayBySubjecType
#      -agentId 848f89f9f2f04317acef00fe1cf1e12e
#      -subjectType "aad"
#      -replayFromDate "2018-06-29" 
#      -replayToDate "2018-06-29" 
#      -includeExportCommands $false
#      -environment PROD
#
function SelfServeReplayBySubjecType
{
    param ([string] $agentId, 
           [ValidateSet("aad","aad2","msa","device","demographic", "microsoftEmployee","nonWindowsDevice","edgeBrowser")] [string] $subjectType,
           [string] $replayFromDate = "", [string] $replayToDate = "", [bool] $includeExportCommands = $false, [string] $environment)
    
    $replayRequest = @{subjectType = $subjectType; replayFromDate = $replayFromDate; replayToDate = $replayToDate; includeExportCommands = $includeExportCommands}
    $requestBody = $replayRequest | ConvertTo-Json

    $response = InvokeS2SWebRequest "debug/replaycommands/$agentId" $environment $true $requestBody

    if ($response.StatusCode -eq 200)
    {
        return "SelfServeReplay requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

#
#  Replay commands for an assetGroupQualifier for a specified date range excluding export commands
#
#  SelfServeReplayByAssetGroupQualifier
#      -agentId 848f89f9f2f04317acef00fe1cf1e12e
#      -assetGroupQualifier "AssetType=ApplicationService;Host=11960d24-92fa-4ca5-9470-2a6e332141fe;Path=Consumer-59608ee1-4cd7-11e8-bacc-c955079f5b12"
#      -replayFromDate "2018-06-29" 
#      -replayToDate "2018-06-29" 
#      -includeExportCommands $false
#      -environment PROD
#
function SelfServeReplayByAssetGroupQualifier
{
    param ([string] $agentId, [string] $assetGroupQualifier,[string] $replayFromDate = "", [string] $replayToDate = "", [bool] $includeExportCommands = $false, [string] $environment)
    
    $assetGroupQualifiers = @($assetGroupQualifier)
    $replayRequest = @{assetQualifiers = $assetGroupQualifiers; replayFromDate = $replayFromDate; replayToDate = $replayToDate; includeExportCommands = $includeExportCommands}
    $requestBody = $replayRequest | ConvertTo-Json

    $response = InvokeS2SWebRequest "debug/replaycommands/$agentId" $environment $true $requestBody

    if ($response.StatusCode -eq 200)
    {
        return "SelfServeReplay requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

#
#  Backfill coldstorage command metadata collection.
#
#  BackfillCommandMetadata -environment PPE
#
function BackfillCommandMetadata
{
    param ([int] $backfillDays = 60, [string] $environment)
    $response = InvokeS2SWebRequest "debug/backfillcommandmetadata?backfillDays = $backfillDays" $environment

    if ($response.StatusCode -eq 200)
    {
        return "Backfill-CommandMetadata requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

#
#  Schedule PCF Command Queue Depth Baseline run for specified agent.
#
#  Start-QueueDepthBaseline -agentId 64e0ebb82dd84c1ea707864566b7e2d9 -environment PPE
#
function Start-QueueDepthBaseline
{
    param ([string] $agentId, [string] $environment)

    $response = InvokeS2SWebRequest "debug/startqdbaseline/$agentId" $environment $true

    if ($response.StatusCode -eq 200)
    {
        return "Queue depth baseline requested"
    }

    $parsedResponse = ConvertFrom-Json $response.Content
    
    return $parsedResponse
}

function InvokeS2SWebRequest
{
    param ([string] $uriSuffix, [string] $environment, [bool] $postMethod = $false, [string] $body = "")

    $environmentInfo = GetEnvironmentConfiguration -environmentType $environment
    $dnsName = $environmentInfo.DnsName

    $cert = gci cert:\localmachine\my | where-object {$_.SubjectName.Name.Contains($environmentInfo.CertificateName) } | Select-Object -First 1

    if ($cert -eq $null)
    {
        throw "Certificate not found. make sure it's installed using provisiondevmachine.ps1"
    }

    # get S2S ticket
    $s2sUri = [Uri]$environmentInfo.S2SUri
    $targetSiteName = $environmentInfo.TargetSiteName      
    $escapedTicketScope = [Uri]::EscapeDataString("$targetSiteName::S2S_24HOURS_MUTUALSSL");
    $s2sResponse = Invoke-WebRequest -Certificate $cert -Uri $s2sUri -Method Post -ContentType "application/x-www-form-urlencoded" -Body "grant_type=client_credentials&client_id=296170&scope=$escapedTicketScope"
    $jsonResponse = ConvertFrom-Json $s2sResponse.Content

    $ticket = $jsonResponse.access_token

    $customHeaders = @{
        "X-S2S-Access-Token" = $ticket
        "X-Stress-Delegated-Auth" = "{ `"AuthenticatedMsaSiteIds`": [ 296170 ] }"
    }

    $requestUri = [Uri]"https://$dnsName/$uriSuffix"

    if($postMethod)
    {
        return Invoke-WebRequest -Certificate $cert -Method POST -Uri $requestUri -Headers $customHeaders -Body $body
    }
    else
    {
        return Invoke-WebRequest -Certificate $cert -Method Get -Uri $requestUri -Headers $customHeaders
    }

}

function GetEnvironmentConfiguration
{
    param ([string] $environmentType)

    [hashtable]$return = @{}

    if ($environmentType -ieq "prod")
    {
        $return.DnsName = "pcf.privacy.microsoft.com"
        $return.TargetSiteName = "pcf.privacy.microsoft.com"
        $return.CertificateName = "sts.pcf.privacy.microsoft-ppe.com"
        $return.S2SUri = "https://login.live.com/pksecure/oauth20_clientcredentials.srf"
    }
    elseif ($environmentType -ieq "ppe")
    {
        $return.DnsName = "pcf.privacy.microsoft-ppe.com"
        $return.TargetSiteName = "pcf.privacy.microsoft-ppe.com"
        $return.CertificateName = "sts.pcf.privacy.microsoft-ppe.com"
        $return.S2SUri = "https://login.live.com/pksecure/oauth20_clientcredentials.srf"
    }    
    elseif ($environmentType -ieq "stress")
    {
        $return.DnsName = "40.90.216.83"
        $return.TargetSiteName = "pcf.privacy.microsoft-int.com"
        $return.CertificateName = "cloudtest.privacy.microsoft-int.ms"
        $return.S2SUri = "https://login.live-int.com/pksecure/oauth20_clientcredentials.srf"
        AvoidSSLValidation
    }
    elseif ($environmentType -ieq "ci1")
    {
        $return.DnsName = "ci1.pcf.privacy.microsoft-int.com"
        $return.TargetSiteName = "pcf.privacy.microsoft-int.com"
        $return.CertificateName = "cloudtest.privacy.microsoft-int.ms"
        $return.S2SUri = "https://login.live-int.com/pksecure/oauth20_clientcredentials.srf"
    }
    elseif ($environmentType -ieq "ci2")
    {
        $return.DnsName = "ci2.pcf.privacy.microsoft-int.com"
        $return.TargetSiteName = "pcf.privacy.microsoft-int.com"
        $return.CertificateName = "cloudtest.privacy.microsoft-int.ms"
        $return.S2SUri = "https://login.live-int.com/pksecure/oauth20_clientcredentials.srf"
    }
    elseif ($environmentType -ieq "sandbox")
    {
        $return.DnsName = "pcf.privacy.microsoft-int.com"
        $return.TargetSiteName = "pcf.privacy.microsoft-int.com"
        $return.CertificateName = "cloudtest.privacy.microsoft-int.ms"
        $return.S2SUri = "https://login.live-int.com/pksecure/oauth20_clientcredentials.srf"
    }
    elseif ($environmentType -ieq "localhost")
    {
        $return.DnsName = "localhost"
        $return.TargetSiteName = "pcf.privacy.microsoft-int.com"
        $return.CertificateName = "cloudtest.privacy.microsoft-int.ms"
        $return.S2SUri = "https://login.live-int.com/pksecure/oauth20_clientcredentials.srf"
        AvoidSSLValidation
    }
    else
    {
        throw "Unrecognized environment type"
    }

    return $return
}

function AvoidSSLValidation
{
    if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type)
    {
        $certCallback = 
@"
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    public class ServerCertificateValidationCallback
    {
        public static void Ignore()
        {
            if(ServicePointManager.ServerCertificateValidationCallback ==null)
            {
                ServicePointManager.ServerCertificateValidationCallback += 
                    delegate
                    (
                        Object obj, 
                        X509Certificate certificate, 
                        X509Chain chain, 
                        SslPolicyErrors errors
                    )
                    {
                        return true;
                    };
            }
        }
    }
"@
        Add-Type $certCallback
    }
    [ServerCertificateValidationCallback]::Ignore()
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12