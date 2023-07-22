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

if ((Test-Path $catalogFolder) -eq $false)
{
	throw "SignedCatalogFiles folder does not exist"
}

Write-Host "Restoring files from $($catalogFolder) to their respecticve service directories."

$catalogFiles = Get-ChildItem -Path $catalogFolder -File -Filter "*.cat"

foreach ($catalogFile in $catalogFiles)
{
	Write-Host "Copying $($catalogFile.FullName) to service directory"

	$baseName = [System.IO.Path]::GetFileNameWithoutExtension($catalogFile.Name)
	$destFolder = [System.IO.Path]::Combine($RootPath, $baseName)

	if ((Test-Path $destFolder) -eq $false)
	{
		throw "Destination Folder $($destFolder) does not exist."
	}

	if (Test-Path "$($destFolder)\$($baseName).cat")
	{
		Write-Host "Overwriting $($destFolder)\$($baseName).cat with version from SignedCatalogFiles"
	}

	Copy-Item -Path $catalogFile.FullName -Destination $destFolder -Force
}