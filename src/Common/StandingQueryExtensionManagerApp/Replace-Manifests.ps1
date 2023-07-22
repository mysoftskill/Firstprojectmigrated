param([string] $src, [string] $appManifestfile, [string] $svcManifestfile, [string]$dest)

# Touch the dest
New-Item -ItemType File -Path $dest -Force
$basename = (Get-Item $dest).Basename
$destDir = (Get-Item $dest).DirectoryName
$zipPath = $destDir + "\" + $basename
$zipFile = $zipPath + ".zip"

# Get destination path for ApplicationManifest.xml and ServiceManifest.xml
$appManifestPath = $zipPath + "\ApplicationManifest.xml"
$svcManifestPath = $zipPath + "\StandingQueryExtensionManagerSvcPkg\ServiceManifest.xml"

# Copy sfpkg to the dest and expand it
Copy-Item $src $zipFile
Expand-Archive $zipFile $zipPath

# Copy updated ApplicationManifest.xml and ServiceManifest.xml
Copy-Item $appManifestfile $appManifestPath -Force
Copy-Item $svcManifestfile $svcManifestPath -Force

# Zip destination folder
$zipPathWithoutRoot = $zipPath + "\*"
Compress-Archive -Path $zipPathWithoutRoot -DestinationPath $zipFile -Force

# Rename to sfpkg
Move-Item $zipFile $dest -Force

Remove-Item -Recurse -Force $zipPath
