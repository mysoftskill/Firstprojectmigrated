param
(
	[Parameter(Mandatory=$true)]
    [string]$Location,
	[Parameter(Mandatory=$true)]
	[string]$Version
)

Get-ChildItem -Path $Location -Filter *App.Parameters.json -Recurse | ForEach-Object { ((Get-Content $_.FullName) -replace "__Version__", $Version) | Set-Content $_.FullName }