# Generate command line parameters for Invoke-OpenApiCSharpAnnotationsDocumentGeneration.ps1

# If you make changes to the XML documentation for an api, you can run this command locally to verify the changes.
#
# To run this command on a devbox, you will need to make sure the XML file has been generated in the appropriate Debug bin dir.
#
# The following commands will ensure that you have all the packages.  Steps 1-4 will generally only need to be done once per repo.
# 
# 1. cd <path to repo>\.build
#
# Add nuget.exe to path
# 2. $env:path = "$env:path;<insert path to nuget.exe>"
#
# restore any needed packages
# 3. .\restore.cmd
#
# Restore the packages needed to run Invoke-OpenApiCSharpAnnotationsDocumentGeneration.ps1
# 4. .\restoreopenapi.cmd
#
# Build everthing (by default, this builds the Debug binaries); you can ignore any services that you are not interested in.
# Alternatively, you can build (in Visual Studio) just the services/projects you are updating.
# 5. .\build.cmd
#
# Set this to the service you want to update (PXS, PCF, PDMS, PCD)
# 6. $env:CDP_BUILD_TAG = "PXS"
#
# Tell the script to look in the Debug release (by default it looks in "Release")
# 7. $env:OPENAPI_BUILD = "Debug"
#
# Build the new document
# 8. .\buildopenapi.cmd
# 
# Ideally, you should check in the new version of OpenApiDocument.OpenApi3_0.json for the service you are updating.

param
(
	[Parameter(Mandatory=$true)]
    [string]$RepoRoot,
	[Parameter(Mandatory=$true)]
	[string]$BuildVersion
)

function ConstructCommandLine($docVersion, $outputPath, $vsXmlPath, $binaryRoot, $binaryList)
{
	$commandLine = "-DocumentVersion ""$docVersion"" -MajorOpenApiSpecificationVersion 3 -MinorOpenApiSpecificationVersion 0 -OutputPath $outputPath "
	$commandLine += "-VisualStudioXmlDocumentationPaths $vsXmlPath -DependentAssembliesDirectoryPath $PSScriptRoot -AssemblyPaths "

	$assemblyPaths = "";
	foreach ($binary in $binaryList)
	{
		$fullPath = Join-Path -Path $binaryRoot -ChildPath $binary
		
		if ($assemblyPaths)
		{
			$assemblyPaths += ","
		}
		$assemblyPaths += """$fullPath"""
	}
	
	$commandLine += $assemblyPaths
	return $commandLine
}

switch ($env:OPENAPI_BUILD)
{
	"Release"  { $Release = "Release" }
	"Debug"  { $Release = "Debug" }
	default { $Release = "Release" }
}

# PXS parameters
$pxsBinaryRoot = Join-Path $RepoRoot "src\PXS\Bin\$Release\x64\PrivacyExperienceService"

$pxsBinaryXmlDoc = Join-Path $pxsBinaryRoot "Microsoft.Membership.MemberServices.PrivacyExperience.Service.xml"

$pxsBinaryList = @(
	"Microsoft.Membership.MemberServices.PrivacyExperience.Service.exe", 
	"Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.dll", 
	"Microsoft.Membership.MemberServices.Privacy.DataContracts.dll", 
	"Microsoft.PrivacyServices.DataSubjectRight.Contracts.dll", 
	"Microsoft.PrivacyServices.PrivacyOperation.Contracts.dll", 
	"Microsoft.PrivacyServices.PXS.Command.Contracts.dll",
	"Newtonsoft.Json.dll"
)

# PCF parameters
$pcfBinaryRoot = Join-Path $RepoRoot "src\PCF\bin\$Release\x64\AutopilotRelease\Frontdoor"

$pcfBinaryXmlDoc = Join-Path $pcfBinaryRoot "Pcf.Frontdoor.xml"

$pcfBinaryList = @(
	"Pcf.Frontdoor.exe",
	"Microsoft.PrivacyServices.PXS.Command.Contracts.dll",
	"Microsoft.PrivacyServices.CommandFeed.Contracts.dll",
    "Newtonsoft.Json.dll"	
)

# PDMS parameters
$pdmsBinaryRoot = Join-Path $RepoRoot "src\PDMS\bin\$Release\x64\ServiceFabricRelease\PdmsFrontdoor"

$pdmsBinaryXmlDoc = Join-Path $pdmsBinaryRoot "PdmsFrontdoor.xml"

$pdmsBinaryList = @(
	"PdmsFrontdoor.exe",
	"PDMS.DataAccess.dll",
	"Microsoft.PrivacyServices.DataManagement.Models.dll"
)

switch ($env:CDP_BUILD_TAG)
{
	"PXS"  { $arguments = ConstructCommandLine $BuildVersion $pxsBinaryRoot $pxsBinaryXmlDoc $pxsBinaryRoot $pxsBinaryList }
	"PCF"  { $arguments = ConstructCommandLine $BuildVersion $pcfBinaryRoot $pcfBinaryXmlDoc $pcfBinaryRoot $pcfBinaryList }
	"PDMS" { $arguments = ConstructCommandLine $BuildVersion $pdmsBinaryRoot $pdmsBinaryXmlDoc $pdmsBinaryRoot $pdmsBinaryList }
	default { Write-Error "CDP_BUILD_TAG is unsupported." }
}

Invoke-Expression "$PSScriptRoot\Invoke-OpenApiCSharpAnnotationsDocumentGeneration.ps1 $arguments"

