#
#  Request replay of PCF comands from tab separated file (tsv)
#   tsv file with headers and include agentid and asset group qualifier divided by tab
# Example of tsv file:
# DeleteAgentId	AssetGroupQualifier
# 57658b1f-7e7f-4514-beab-101eacbd69a2	AssetType=SqlServer;ServerName=msanalyticsdevsql2.redmond.corp.microsoft.com;DatabaseName=WorkDB_Store
# 72b1e37b-4ab2-4698-8c5a-3073b7c42181	AssetType=SqlServer;ServerName=MSAnalyticsWHSe03.corp.microsoft.com;DatabaseName=Dashboards
#
# Prereq: . ".\DebugScripts.ps1"
#
#   How to run:
#  .\Request-AssetGroupQualifierReplayFromTsv.ps1
#      -Path "C:\Users\rupavlen\Documents\20200205 Replay.txt"
#      -FromDate "2020-02-03" 
#      -ToDate "2020-02-03" 
#      -environment PROD
#
param ([string] $Path, [string] $FromDate, [string] $ToDate, [string] $Environment)

$ErrorActionPreference = "Stop"

Write-Host "Replay from: $FromDate to: $ToDate environment: $Environment"
$replays = Import-Csv -Path $Path -Delimiter "`t"
foreach ($replay in $replays)
{
    SelfServeReplayByAssetGroupQualifier -agentId $replay.DeleteAgentId -assetGroupQualifier $replay.AssetGroupQualifier -replayFromDate $FromDate -replayToDate $ToDate -environment $Environment
    if (!$?)
    {
        throw "Fail to request replay for $($replay.DeleteAgentId) `t $($replay.AssetGroupQualifier)"
    }
    Write-Host "[REQUESTED] [$FromDate - $ToDate]: $($replay.DeleteAgentId)`t$($replay.AssetGroupQualifier)`t"
}
