# Ideally the arguments would pass in this environment variable, but ServiceFabric doesn't resolve them. 
# This is just one of many different possible workarounds. The value must match the env variable defined in the ServiceManifest.xml
$environmentType = $env:PDMS_EnvironmentName
Write-Host "Targeting environment: " $env:PDMS_EnvironmentName

if ([string]::IsNullOrWhiteSpace($environmentType))
{
    $error = "The environment type needs to be specified. Value was NullOrWhiteSpace"
    Write-Host $error
    throw $error
}

"[UserProperty]`r`nServiceFabric=true`r`nenvnamespace=$environmentType`r`nIsDocdbAutoscaleMaster=true" | out-file -encoding ASCII props.txt

Get-ChildItem . |
	Where-Object { $_.Name.EndsWith(".ini") -and !$_.Name.EndsWith("flattened.ini") } |
	ForEach-Object {
		./IniFlatten.exe -i $_ -o "$_.flattened.ini" -p ./props.txt
	}
