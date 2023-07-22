Param
(
    [Parameter(Mandatory=$true)]
    [String]
    $RootPath
)

if ((Test-Path $RootPath) -eq $false)
{
    throw "Invalid Root Path"
}

$catalogFolder = [System.IO.Path]::Combine($RootPath, "SignedCatalogFiles")

if (Test-Path $catalogFolder)
{
	Remove-Item -Force -Recurse -Path $catalogFolder
}

New-Item -ItemType Directory -Path $catalogFolder

$directories = get-childitem $RootPath -Directory | Where-Object { $_.Name -ne "SignedCatalogFiles" }

foreach ($directory in $directories)
{
	$catalogFile = "$($directory.FullName)\$($directory.Name).cat"
	if (Test-Path $catalogFile)
	{
		Write-Host "Copying $($catalogFile) to $($catalogFolder)"
		Copy-Item -Path $catalogFile -Destination $catalogFolder
	}
}