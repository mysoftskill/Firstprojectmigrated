param
(
	[Parameter(Mandatory=$true)]
    [string]$Location,
	[Parameter(Mandatory=$true)]
	[string]$Version,
    [Parameter(Mandatory=$true)]
    [string]$OneBranchDir
)
# Removes leading zeros by removing one or more "0"s following a word boundary "." and not followed by a word boundary "."

# Ex: 019.001.000.1 -> 19.1.0.1 This is required because Service Fabric requires no leading zeroes for builds
$VersionNoLeadingZeros = $Version -replace '\b0+\B'


$VersionNoLeadingZeros | out-file -encoding utf8 -filepath $OneBranchDir\Deployment\BuildVer.txt
Get-ChildItem -Path $Location -Filter *App.Parameters.json -Recurse | ForEach-Object { ((Get-Content $_.FullName) -replace "__Version__", $VersionNoLeadingZeros) | Set-Content $_.FullName } 
