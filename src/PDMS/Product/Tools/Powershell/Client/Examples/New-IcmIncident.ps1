using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
##############################################################################
#.SYNOPSIS
# Finds all asset groups that are linked to a specific agent id.
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER Id
# The variant id to search for.
#
#.PARAMETER Location
# One of the following values: INT, PPE, PROD
#
#.EXAMPLE
# .\New-IcmIncident.ps1 -AgentIds .\bad-agents-pcf.csv -IdsFromFile -EventName pcd.UnresponsiveAgent -Title 'NGP PCF Agent has not processed any Commands in 10 days [READ DESCRIPTION FOR INSTRUCTIONS]' -Body .\noAckAgent.html -BodyFromFile -Keywords "NGPDataAgentLivesite" -Location PROD -Severity 2
##############################################################################
[cmdletbinding(DefaultParameterSetName=’ownerSource’)]
param(
    [Parameter(ParameterSetName='ownerSource', Mandatory=$true, Position=0)]
    [Parameter(ParameterSetName='agentSource', Position=6)]
    [Parameter(ParameterSetName='assetGroupSource', Position=6)]
	[string]
	$OwnerId,

    [Parameter(ParameterSetName='ownerSourceArray', Mandatory=$true, Position=0)]
	[string[]]
	$OwnerIds,

    [Parameter(ParameterSetName='ownerSource', Position=7)]
    [Parameter(ParameterSetName='agentSource', Mandatory=$true, Position=0)]
    [Parameter(ParameterSetName='assetGroupSource', Position=7)]
	[string]
	$AgentId,

    [Parameter(ParameterSetName='agentSourceArray', Mandatory=$true, Position=0)]
	[string[]]
	$AgentIds,

    [Parameter(ParameterSetName='ownerSource', Position=8)]
    [Parameter(ParameterSetName='agentSource', Position=8)]
    [Parameter(ParameterSetName='assetGroupSource', Mandatory=$true, Position=0)]
	[string]
	$AssetGroupId,

    [Parameter(ParameterSetName='assetGroupSourceArray', Mandatory=$true, Position=0)]
	[string[]]
	$AssetGroupIds,

    [Parameter(ParameterSetName='ownerSourceArray')]
    [Parameter(ParameterSetName='agentSourceArray')]
    [Parameter(ParameterSetName='assetGroupSourceArray')]
	[switch]
	$IdsFromFile,

	[parameter(Mandatory=$true, Position=1)]
	[string]
	$EventName,

	[parameter(Mandatory=$true, Position=2)]
	[string]
	$Title,

	[parameter(Mandatory=$true, Position=3)]
	[string]
	$Body,

	[parameter(Mandatory=$false, Position=4)]
	[switch]
	$BodyFromFile,

	[parameter(Mandatory=$false, Position=5)]
	[int]
	$Severity=4,

	[parameter(Mandatory=$false, Position=6)]
	[string]
	$Keywords,

	[string]
	[parameter(Mandatory=$true, Position=9)]
	$Location,

    [switch]
	$Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

$count = 0
$sentIncidents = @()
$failedIncidents = @()

$updatedResultFile = "$($PSScriptRoot)\Incidents\sentIncidents.txt"
$failedResultFile = "$($PSScriptRoot)\Incidents\failedIncidents.txt"

if ($OwnerIds) {
    if ($IdsFromFile) {
        $OwnerIds = Import-Csv $OwnerIds | ForEach-Object { $_.OwnerId }
    }

    $OwnerIds | ForEach-Object {
        $OwnerId = $_
        try {
            $i = New-PdmsObject Incident
            Set-PdmsProperty $i Title $Title
            Set-PdmsProperty $i Severity $Severity
    
            if ($Keywords) {
	            Set-PdmsProperty $i Keywords $Keywords
            }

            if ($BodyFromFile) {
                Set-PdmsProperty $i Body (Get-Content $Body -Raw)
            }
            else {
                Set-PdmsProperty $i Body $Body
            }

            $r = New-PdmsObject RouteData

            if ($OwnerId) {
                Set-PdmsProperty $r OwnerId ([GUID]$OwnerId)
            }

            if ($AgentId) {
                Set-PdmsProperty $r AgentId ([GUID]$AgentId)
            }

            if ($AssetGroupId) {
                Set-PdmsProperty $r AssetGroupId ([GUID]$AssetGroupId)
            }

            Set-PdmsProperty $r EventName $EventName
            Set-PdmsProperty $i Routing $r
        
            $r

            if ($Force) {
                $result = New-PdmsIncident $i -ErrorAction Stop
            }
            else {
                $result = $i
            }

            $result
            $count += 1
            "-----------------------------"
            $count
            "-----------------------------"

            $sentIncidents += $result
        }
        catch [Exception]{
            Write-Warning $_
            $failedIncidents += $r
        }
    }
}
elseif ($AgentIds) {
    if ($IdsFromFile) {
        $AgentIds = Import-Csv $AgentIds | ForEach-Object { $_.AgentId }
    }

    $AgentIds | ForEach-Object {
        $AgentId = $_
        try {
            $i = New-PdmsObject Incident
            Set-PdmsProperty $i Title $Title
            Set-PdmsProperty $i Severity $Severity
    
            if ($Keywords) {
	            Set-PdmsProperty $i Keywords $Keywords
            }

            if ($BodyFromFile) {
                Set-PdmsProperty $i Body (Get-Content $Body -Raw)
            }
            else {
                Set-PdmsProperty $i Body $Body
            }

            $r = New-PdmsObject RouteData

            if ($OwnerId) {
                Set-PdmsProperty $r OwnerId ([GUID]$OwnerId)
            }

            if ($AgentId) {
                Set-PdmsProperty $r AgentId ([GUID]$AgentId)
            }

            if ($AssetGroupId) {
                Set-PdmsProperty $r AssetGroupId ([GUID]$AssetGroupId)
            }

            Set-PdmsProperty $r EventName $EventName
            Set-PdmsProperty $i Routing $r
        
            $r

            if ($Force) {
                $result = New-PdmsIncident $i -ErrorAction Stop
            }
            else {
                $result = $i
            }

            $result
            $count += 1
            "-----------------------------"
            $count
            "-----------------------------"

            $sentIncidents += $result
        }
        catch [Exception]{
            Write-Warning $_
            $failedIncidents += $r
        }
    }
}
elseif ($AssetGroupIds) {
    if ($IdsFromFile) {
        $AssetGroupIds = Import-Csv $AssetGroupIds | ForEach-Object { $_.AssetGroupId }
    }

    $AssetGroupIds | ForEach-Object {
        $AssetGroupId = $_
        try {
            $i = New-PdmsObject Incident
            Set-PdmsProperty $i Title $Title
            Set-PdmsProperty $i Severity $Severity
    
            if ($Keywords) {
	            Set-PdmsProperty $i Keywords $Keywords
            }

            if ($BodyFromFile) {
                Set-PdmsProperty $i Body (Get-Content $Body -Raw)
            }
            else {
                Set-PdmsProperty $i Body $Body
            }

            $r = New-PdmsObject RouteData

            if ($OwnerId) {
                Set-PdmsProperty $r OwnerId ([GUID]$OwnerId)
            }

            if ($AgentId) {
                Set-PdmsProperty $r AgentId ([GUID]$AgentId)
            }

            if ($AssetGroupId) {
                Set-PdmsProperty $r AssetGroupId ([GUID]$AssetGroupId)
            }

            Set-PdmsProperty $r EventName $EventName
            Set-PdmsProperty $i Routing $r
        
            $r

            if ($Force) {
                $result = New-PdmsIncident $i -ErrorAction Stop
            }
            else {
                $result = $i
            }

            $result
            $count += 1
            "-----------------------------"
            $count
            "-----------------------------"

            $sentIncidents += $result
        }
        catch [Exception]{
            Write-Warning $_
            $failedIncidents += $r
        }
    }
}
else {
    try {
        $i = New-PdmsObject Incident
        Set-PdmsProperty $i Title $Title
        Set-PdmsProperty $i Severity $Severity
    
        if ($Keywords) {
	        Set-PdmsProperty $i Keywords $Keywords
        }

        if ($BodyFromFile) {
            Set-PdmsProperty $i Body (Get-Content $Body -Raw)
        }
        else {
            Set-PdmsProperty $i Body $Body
        }

        $r = New-PdmsObject RouteData

        if ($OwnerId) {
            Set-PdmsProperty $r OwnerId ([GUID]$OwnerId)
        }

        if ($AgentId) {
            Set-PdmsProperty $r AgentId ([GUID]$AgentId)
        }

        if ($AssetGroupId) {
            Set-PdmsProperty $r AssetGroupId ([GUID]$AssetGroupId)
        }

        Set-PdmsProperty $r EventName $EventName
        Set-PdmsProperty $i Routing $r
        
        $r

        if ($Force) {
            $result = New-PdmsIncident $i -ErrorAction Stop
        }
        else {
            $result = $i
        }

        $result
        $count += 1
        "-----------------------------"
        $count
        "-----------------------------"

        $sentIncidents += $result
    }
    catch [Exception]{
        Write-Warning $_
        $failedIncidents += $r
    }
}

'--------------- Total Incidents ---------------------'
$sentIncidents.Count + $failedIncidents.Count

'--------------- Sent Incidents ---------------------'
New-Item -Force -Path $updatedResultFile -Type file
if ($sentIncidents.Count -ne 0) { ConvertTo-PdmsJson -Value $sentIncidents | Out-File $updatedResultFile }
$sentIncidents.Count

'--------------- Failed Incidents ---------------------'
New-Item -Force -Path $failedResultFile -Type file
if ($failedIncidents.Count -ne 0) { ConvertTo-PdmsJson -Value $failedIncidents | Out-File $failedResultFile }
$failedIncidents.Count
  
if ($Force -eq $false) {
	Write-Warning "Running in PREVIEW mode. No changes were made. Re-run with -Force parameter to apply these changes."
}