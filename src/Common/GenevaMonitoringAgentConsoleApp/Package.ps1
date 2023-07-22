param(
    [string] $OutputPath,
    [string] $PackagesDir,
    [string] $DropPath,
    [string] $TargetPackageName,
    [string] $MAPkgVersion,
    [string] $MonitoringAppPkgVersion
)

Write-Host "OutputPath: $OutputPath"
Write-Host "PackagesDir: $PackagesDir"
Write-Host "DropPath: $DropPath"
Write-Host "TargetPackageName: $TargetPackageName"
Write-Host "MAPkgVersion: $MAPkgVersion"
Write-Host "MonitoringAppPkgVersion: $MonitoringAppPkgVersion"


$SFGenevaNuget = "Microsoft.ServiceFabric.Geneva\$MonitoringAppPkgVersion"
$SFGenevaNugetPath = Join-Path $PackagesDir $SFGenevaNuget
$MultiTenantMonitoringServiceWithMAPath = Join-Path $SFGenevaNugetPath "MultiTenantMonitoringServiceWithMA\"
$TenantConfigFilesPath = Join-Path $OutputPath "Tenants\*"
$TargetPackagePath = "$DropPath\Monitoring\$TargetPackageName"

Remove-Item $TargetPackagePath -Force -Recurse -ErrorAction Ignore
Copy-Item $MultiTenantMonitoringServiceWithMAPath -Destination $TargetPackagePath -Recurse -Force
Copy-Item $TenantConfigFilesPath -Destination "$TargetPackagePath\MdsAgentServicePackage\MdsAgent.Data\Tenants" -Recurse -Force