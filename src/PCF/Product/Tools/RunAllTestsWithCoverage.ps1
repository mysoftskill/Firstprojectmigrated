# This script builds the current solution, and runs code coverage on both unit and functional test cases
# resulting in a merged output that shows the actual comprehensive coverage of our test cases.

# the directory of this script.
$scriptDirectory = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)

# Change to the "product" directory.
$productDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptDirectory, "..\"));

$buildDirectory = [System.IO.Path]::Combine($productDirectory, "Build");

function InstrumentCurrentDirectory
{
    $vsInstrPath = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Team Tools\Performance Tools\x64\vsinstr.exe";

    $searchPatterns = @("*Pcf*.dll", "*Pcf*.exe", "Microsoft.PrivacyServices.CommandFeed.Client.dll", "Microsoft.PrivacyServices.CommandFeed.Validator.dll");

    foreach ($pattern in $searchPatterns)
    {
        foreach ($item in gci -Path $pattern)
        {
            $fileName = [System.IO.Path]::GetFileName($item.FullName)

            if ($fileName.Contains("Tests"))
            {
                # Exclude tests from code coverage
                continue;
            }

            & $vsInstrPath $fileName /COVERAGE 2>&1

            # attempt to re-sign the assembly, if appropriate. This is a no-op for non-signed assemblies.
            $fullName = $item.FullName
            & sn -Ra "$fullName" "$buildDirectory\UnprotectedKey.snk"
        }
    }
}


$frontDoorBinaryDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($productDirectory, "..\bin\Debug\x64\AutopilotRelease\Frontdoor\"));
$workerBinaryDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($productDirectory, "..\bin\Debug\x64\AutopilotRelease\PcfWorker\"));
$dmsBinaryDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($productDirectory, "..\bin\Debug\x64\AutopilotRelease\DmsServer\"));
$unitTestDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($productDirectory, "..\bin\Debug\x64\UnitTests\"));
$clientUnitTestDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($productDirectory, "..\bin\Debug\x64\PrivacyCommandProcessor.UnitTest\"));
$functionalTestDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($productDirectory, "..\bin\Debug\x64\FunctionalTests\"));
$vsPerfCmd = "C:\Program Files (x86)\Microsoft Visual Studio\Shared\Common\VSPerfCollectionTools\x64\VSPerfCmd.exe"
$vsTest = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"

Push-Location $productDirectory

# rebuild the sources
Remove-Item -Force -Recurse -Path $frontDoorBinaryDirectory
Remove-Item -Force -Recurse -Path $workerBinaryDirectory
Remove-Item -Force -Recurse -Path $dmsBinaryDirectory
Remove-Item -Force -Recurse -Path $unitTestDirectory
Remove-Item -Force -Recurse -Path $functionalTestDirectory
Remove-Item -Force -Recurse -Path $clientUnitTestDirectory
.\build.cmd debug

# Instrument binaries for code coverage
Push-Location $frontDoorBinaryDirectory
InstrumentCurrentDirectory

Push-Location $workerBinaryDirectory
InstrumentCurrentDirectory

Push-Location $dmsBinaryDirectory
InstrumentCurrentDirectory

Push-Location $unitTestDirectory
InstrumentCurrentDirectory

Push-Location $functionalTestDirectory
InstrumentCurrentDirectory

Push-Location $clientUnitTestDirectory
InstrumentCurrentDirectory

# attach code coverage
Start-Process -FilePath $vsPerfCmd -ArgumentList "/START:COVERAGE", "/OUTPUT:$scriptDirectory\coverage.coverage"
Start-Sleep -Seconds 1

# Execute unit tests while waiting for the frontdoor to initialize.
Push-Location $unitTestDirectory
& $vsTest Pcf.UnitTests.dll /Platform:x64 /EnableCodeCoverage 2>&1

# start our processes
Push-Location $dmsBinaryDirectory
$p = Start-Process Pcf.DmsService.exe

Push-Location $workerBinaryDirectory
$p = Start-Process Pcf.Worker.exe

Push-Location $frontDoorBinaryDirectory
$p = Start-Process Pcf.Frontdoor.exe

# Execute client unit tests
Push-Location $clientUnitTestDirectory
& $vsTest PrivacyCommandProcessor.UnitTests.dll /Platform:x64 /EnableCodeCoverage 2>&1

# change directory
Push-Location $functionalTestDirectory

# Make sure the process has loaded
while ($true)
{
    try
    {
        $response = Invoke-WebRequest https://localhost/keepalive

        if ($response.StatusCode -eq 200)
        {
            break;
        }
    }
    catch { Start-Sleep -Seconds 1 }
}

Start-Sleep -Seconds 1

# Execute the VS test runner on the FCT dll.
& $vsTest Pcf.FunctionalTests.dll /Platform:x64 /EnableCodeCoverage 2>&1

taskkill /F /IM Pcf.Frontdoor.exe
taskkill /F /IM Pcf.Worker.exe
taskkill /F /IM Pcf.DmsService.exe

# shut down VS perf mon
& $vsPerfCmd /SHUTDOWN

Write-Host -ForegroundColor Green "Code coverage results are availabe at $scriptDirectory\coverage.coverage"