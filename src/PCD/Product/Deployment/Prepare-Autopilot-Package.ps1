<#
.Synopsis
    Combines all necessary binaries and creates a package for Autopilot deployment.

.Description
    This script picks up web role build output, AutoIIS, certificate manager, configuration files
    and additional dependencies, then copies them all up into Autopilot release folder,
    ready to be deployed.

.Parameter webRoleBitsFolder
    Path to web role bits (result of dotnet publish command).
.Parameter $outputFolder
    Path to the folder where Autopilot release is going to be created.
.Parameter $IsDevbox
    Whether this is a devbox test build or not.

.Example
    .\Prepare-Autopilot-Package.ps1 -webRoleBitsFolder C:\temp\ux -outputFolder .
#>

param (
    [Parameter(Mandatory = $true)][string] $webRoleBitsFolder,
    [Parameter(Mandatory = $true)][string] $outputFolder,
	[Parameter(Mandatory = $false)][switch]$IsDevbox = $false
)

..\Build\Invoke-Environment "..\Build\buildenv.cmd"

$cwd = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)

$dropFolder = [System.IO.Path]::Combine($outputFolder, "Drop")
$dropWebRoleFolder = [System.IO.Path]::Combine($dropFolder, "pdmsux")
$dropAutoIisFolder = [System.IO.Path]::Combine($dropFolder, "ApAutoIIS")
$dropCertificateManagementFolder = [System.IO.Path]::Combine($dropFolder, "CertificateManagement")

$testingFolder = [System.IO.Path]::Combine($outputFolder, "Testing")
$testingScriptsFolder = [System.IO.Path]::Combine($testingFolder, "Scripts")

& ..\Build\tools\nuget\nuget restore deployment.proj -PackagesDirectory "${env:NugetPackages}"

# Copy everything to the drop folder.
if (!$IsDevbox) {
    robocopy "${env:Build_SourcesDirectory}\Product\Source\CertInstaller\bin\${env:BuildConfiguration}" "$dropCertificateManagementFolder" /s /np /nfl /ndl /is /it
} else {
    robocopy "..\Source\certinstaller\bin\Debug" "$dropCertificateManagementFolder" /s /np /nfl /ndl /is /it
}

robocopy "${env:NugetPackages}\ApAutoIIS\10.8.5000.1632\ApAutoIIS" "$dropAutoIisFolder" /s /np /nfl /ndl /is /it
robocopy "${env:NugetPackages}\Microsoft.MeePortal.Deployment.ApSecretStore.Library\2018.12.19\native" "$dropAutoIisFolder" /s /np /nfl /ndl /is /it
robocopy "${env:NugetPackages}\Microsoft.MeePortal.Deployment.ApSecretStore.Library\2018.12.19\lib\net40" "$dropAutoIisFolder" /s /np /nfl /ndl /is /it
robocopy "$([System.IO.Path]::Combine($cwd, "Configs"))" "$dropFolder" /s /np /nfl /ndl /is /it
robocopy "$webRoleBitsFolder" "$dropWebRoleFolder" /s /np /nfl /ndl /is /it /mt
robocopy "$([System.IO.Path]::Combine($cwd, "AutoIIS"))" "$dropWebRoleFolder" /s /np /nfl /ndl /is /it

robocopy "${env:NugetPackages}\Microsoft.PrivacyServices.UX.Deployment.IisRewrite\1.0.1" "$dropWebRoleFolder\redist" /s /np /nfl /ndl /is /it /xf *.nupkg
robocopy "${env:NugetPackages}\Microsoft.PrivacyServices.UX.Deployment.IisAspNetCoreModule\2.1.11" "$dropWebRoleFolder\redist" /s /np /nfl /ndl /is /it /xf *.nupkg

robocopy "$([System.IO.Path]::Combine($cwd, "..\Scripts\Testing"))" "$testingScriptsFolder" /s /np /nfl /ndl /is /it

# Robocopy returns a wide variety of exit codes - https://ss64.com/nt/robocopy-exit.html
# Hardcode exit code to 0 to not to break a build.
exit 0
