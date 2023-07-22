#-------------------------------------------------------------------------------------------
# This script will query the build service and download the code
# coverage results file. It then analyzes that data and calculates
# a code coverage percentage value. If that value is less than 
# the provided minimum requirement, the script exists with a failure.
#
# You can integrate this check into VSO by creating a new build step.
# This build step needs to be at the end to ensure the results are published in time.
# You also need to check the "enable code coverage" checkbox on the unit test step.
#
# Additionally, in order for the authentication to work properly,
# you need to change the build definition options to allow scripts access to the OAuth token.
#-------------------------------------------------------------------------------------------

param(
  # Getting the control percentage as an argument
  # This Default value will be overriden by the parameter set on Build Definition
  [int]$desiredCodeCoveragePercent = 35
)

$ErrorActionPreference = “Stop”

Function QueryResults { 
  # Setting a few values
  [int]$coveredBlocks = 0
  [int]$skippedBlocks = 0
  [int]$totalBlocks = 0
  [int]$codeCoveragePercent = 0

  # Getting a few environment variables we need
  [String]$buildID = "$env:BUILD_BUILDID"
  [String]$project = "$env:SYSTEM_TEAMPROJECT"

  $basicAuth = ("{0}:{1}" -f $username, $password)
  $basicAuth = [System.Text.Encoding]::UTF8.GetBytes($basicAuth)
  $basicAuth = [System.Convert]::ToBase64String($basicAuth)

  $headers = @{ Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN" }
  $url = "https://microsoft.visualstudio.com/DefaultCollection/" + $project + "/_apis/test/codeCoverage?buildId=" + $buildID + "&flags=1&api-version=2.0-preview"
  Write-Host $url 
   
  $responseBuild= (Invoke-RestMethod -Uri $url -headers $headers -Method Get).value | select modules

  foreach ($module in $responseBuild.modules)
  {
    $coveredBlocks += $module.statistics[0].blocksCovered
    $skippedBlocks += $module.statistics[0].blocksNotCovered
  }
    
  $totalBlocks = $coveredBlocks + $skippedBlocks;
  if ($totalBlocks -eq 0)
  {
     $codeCoveragePercent = 0
  }
  else
  {    
    $codeCoveragePercent = $coveredBlocks * 100.0 / $totalBlocks
  }
  
  Write-Host "Code Coverage percentage is " -nonewline; Write-Host $codeCoveragePercent
  return $codeCoveragePercent;
}

Write-Host "Desired Code Coverage Percent is " -nonewline; Write-Host  $desiredCodeCoveragePercent

$codeCoveragePercent = 0;
$attempts = 30; # Results in 5 min wait.

do
{
  $codeCoveragePercent = QueryResults;

  if ($codeCoveragePercent -eq 0) {
    Start-Sleep -s 10
    $attempts--;
  }
} while ($codeCoveragePercent -eq 0 -And $attempts -ge 0)

if ($codeCoveragePercent -lt $desiredCodeCoveragePercent)
{
   Write-Host "Failing the build as CodeCoverage limit not met"
   exit -1
}
Write-Host "CodeCoverage limit met"