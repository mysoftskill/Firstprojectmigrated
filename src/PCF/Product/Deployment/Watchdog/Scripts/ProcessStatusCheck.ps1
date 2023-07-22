Param(
    [Parameter(Mandatory=$true)]
    [string]$processName,
    [Parameter(Mandatory=$true)]
    [int]$minRunningSeconds
)

Function Update-DMProperty {
    [CmdletBinding()]
    Param(
    [Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, Position=0)]
    [string]$hostName,
    [Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, Position=1)]
    [string]$dmStatus,
    [Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, Position=2)]
    [string]$dmPropertyName,
    [Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, Position=3)]
    [string]$dmPropertyValue,
    [Parameter(Mandatory=$false, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, Position=4)]
    [switch]$Pretend
    )

    $dmPropertyValue = "[{0}]{1}" -f $env:computername, $dmPropertyValue
    
    $dmStatusFile = ("{0}\{1}.csv" -f $env:DataDir, $dmPropertyName)
    ("{0},{1},{2},{3}" -f $hostName, $dmStatus, $dmPropertyName, $dmPropertyValue) | out-file -encoding Default $dmStatusFile -force

    Write-Verbose "[Update-DMProperty] $hostName $dmStatus $dmPropertyName $dmPropertyValue"
        
    if($Pretend -ne $true) {
        $apToolsDir = Get-ChildItem ("{0}\aptools.ap*" -f $env:AppRoot)
        $params = ("-c updatemachineproperty -i '{0}'" -f $dmStatusFile)

        push-location $apToolsDir[0]
        $PathToExecutable = '.\dmclient.exe'
        Invoke-Expression -command "$PathToExecutable $params" | out-host
        pop-location
    }

     <#
     .SYNOPSIS
        Update machines property through DMclient
     .DESCRIPTION
        The Update-DMProperty cmdlet call DMClient.exe to update machine property with the supplied parameters.
     .INPUTS
        hostName Specifies the host name.
        dmStatus Specifies status of the machines property
        dmPropertyName Specifies property name
        dmPropertyValue Specifies value/description of the property
    .OUTPUTS
        None.
     .EXAMPLE
        Update-DMProperty 'MW1PEPF00000288' 'OK' 'PCF_Watchdog' 'PCF service is OK'
    #>
}

Write-Host "Getting Status of Process $processName..."
$process = Get-Process $processName -ErrorAction SilentlyContinue

if ($process -eq $null)
{
    $errorDesc = "Process $processName does not exist. Sending Error to DMClient."
    Write-Host -ForegroundColor Red $errorDesc
    Update-DMProperty $env:computername 'Error' 'PCF_PsWatchdog' $errorDesc
    exit 1
}
else
{
    $runningTime = ((Get-Date) - $process.StartTime).TotalSeconds;
    if ($process.HasExited -or ($runningTime -lt $minRunningSeconds))
    {
        $errorDesc = "Process $processName hasn't running longer than $minRunningSeconds seconds. Sending Error to DMClient."
        Write-Host -ForegroundColor Red $errorDesc
        Update-DMProperty $env:computername 'Error' 'PCF_PsWatchdog' $errorDesc
        exit 1
    }
    else
    {
        Write-Host "Process $processName is running good. Sending OK to DMClient."
        Update-DMProperty $env:computername 'OK' 'PCF_PsWatchdog' 'OK'
        exit 0
    }
}