# Ideally the arguments would pass in this environment variable, but ServiceFabric doesn't resolve them. 
# This is just one of many different possible workarounds. The value must match the env variable defined in the ServiceManifest.xml
$environmentType = $env:PCD_EnvironmentName
Write-Host "Targeting environment: " $env:PCD_EnvironmentName

if ([string]::IsNullOrWhiteSpace($environmentType))
{
    $error = "The environment type needs to be specified. Value was NullOrWhiteSpace"
    Write-Host $error
    throw $error
}

"[UserProperty]`r`nServiceFabric=true`r`nenvnamespace=$environmentType`r`nenvtype=$environmentType" | out-file -encoding ASCII props.txt
#Flattens all file recursively in this directory
Get-ChildItem -Recurse. |
	Where-Object { $_.Name.EndsWith(".ini") -and !$_.Name.EndsWith("flattened.ini") } |
	ForEach-Object {
		./IniFlatten.exe -i $_.FullName -o "$($_.FullName).flattened.ini" -p ./props.txt
	}
