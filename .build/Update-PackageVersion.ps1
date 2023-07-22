param
(
	[Parameter(Mandatory=$true)]
    [string]$PackageLocation,
	[Parameter(Mandatory=$true)]
	[string]$Version
)

#Update Service manifest/CodePackage/ConfigPackage versions in service manifest file
function UpdateServiceManifests($version)
{
    $applicationManifest = "$($PackageLocation)\\ApplicationManifest.xml"
    $applicationManifestXml = (Select-Xml -Path $applicationManifest -XPath /).Node
    $serviceManifestNames = $applicationManifestXml.ApplicationManifest.ServiceManifestImport.ServiceManifestRef.ServiceManifestName

    foreach($item in $serviceManifestNames)
    {
        $serviceManifest = "$($PackageLocation)\\$($item)\\ServiceManifest.xml"
        $serviceManifestXml = (Select-Xml -Path $serviceManifest -XPath /).Node
        $ServiceManifestXml.ServiceManifest.Version = $version
        if ($ServiceManifestXml.ServiceManifest.PSObject.Properties['DataPackage'])
        {
            $ServiceManifestXml.ServiceManifest.DataPackage.Version = $version
        }
        $serviceManifestXml.ServiceManifest.CodePackage.Version = $version
        $serviceManifestXml.ServiceManifest.ConfigPackage.Version = $version
        $serviceManifestXml.Save($serviceManifest)
    }
}

#Update ApplicationTypeVersion/ServiceManifestVersion versions in Application manifest file
function UpdateApplicationManifest($version)
{
    $applicationManifest = "$($PackageLocation)\\ApplicationManifest.xml"
    $applicationManifestXml = (Select-Xml -Path $applicationManifest -XPath /).Node
    $applicationManifestXml.ApplicationManifest.ApplicationTypeVersion = $version
    $applicationManifestXml.ApplicationManifest.ServiceManifestImport.ServiceManifestRef | ForEach-Object{$_.ServiceManifestVersion=$version}
    $applicationManifestXml.Save($applicationManifest)
}

Write-Output "Updating version in Application Manifest...with $Version"
UpdateApplicationManifest -version $Version
Write-Output "Updating version in Service Manifest... with $Version"
UpdateServiceManifests -version $Version