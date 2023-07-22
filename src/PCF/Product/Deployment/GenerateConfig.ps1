# Run from the directory where the .ps1 is located. This allows relative paths to work correctly.

param ([string] $packageLocations)

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

$genConfigPath = $null

foreach ($testPath in $packageLocations.Split(";"))
{
    $test = [System.IO.Path]::Combine($testPath, "Genconfig\1.0.2860\tools\genconfig.cmd")
    if (Test-Path $test)
    {
        $genConfigPath = $test
        break
    }
}

if ($genConfigPath -eq $null)
{
    throw "Unable to find genconfig nuget package.";
}

Write-Host "Found GenConfig at '$genConfigPath'"

Push-Location $dir
$oldLocation = [System.Environment]::CurrentDirectory
[System.Environment]::CurrentDirectory = $dir

try
{
    function Hash($textToHash)
    {
        $hasher = new-object System.Security.Cryptography.SHA256Managed
        $toHash = [System.Text.Encoding]::UTF8.GetBytes($textToHash)
        $hashByteArray = $hasher.ComputeHash($toHash)
        return [System.BitConverter]::ToInt64($hashByteArray, 0);
    }

    # calculate the hash of the .txt files and the .tpl files. We compare these against the last known
    # version of the generated config file. This allows us to short-circuit for cases where it didn't change.
    $hash = 0
    foreach ($item in (gci -Recurse config\*.txt))
    {
        $content = Get-Content $item

        if ($content -eq $null)
        {
            $content = ""
        }

        $contentHash = Hash($content)
        $hash = $hash -bxor $contentHash
    }

    foreach ($item in (gci -Recurse config\*.tpl))
    {    
        $content = Get-Content $item

        if ($content -eq $null)
        {
            $content = ""
        }
    
        $contentHash = Hash($content)
        $hash = $hash -bxor $contentHash
    }

    # compare the hash against what we generated last time
    if (Test-Path .\Generated\configstamp.txt)
    {
        $stamp = (Get-Content .\generated\configstamp.txt).Trim()
        if ($hash.ToString() -eq $stamp)
        {
            # config has not changed; hashes match. Just return!
            [System.Console]::WriteLine("Config has not changed; short ciruiting...");
            return;
        }
    }

    # We got here, so something changed. We need to do a complete regeneration of our configuration.
    # First, apply overrides on top of the template.

    # PROD
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXS-PROD-BN3P.config /data:Config\common.txt /data:Config\prod.txt"
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXS-PROD-BY3P.config /data:Config\common.txt /data:Config\prod.txt"
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXS-PROD-SN3P.config /data:Config\common.txt /data:Config\prod.txt"
    
    # PPE
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXS-PPE-MW1P.config /data:Config\common.txt /data:Config\prod.txt /data:Config\ppe.txt"
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXS-PPE-SN3P.config /data:Config\common.txt /data:Config\prod.txt /data:Config\ppe.txt"
    
    # CI
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXSCI1-Test-MW1P.config /data:Config\common.txt /data:Config\prod.txt /data:Config\int.txt /data:Config\PXSCI1-Test-MW1P.txt"
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXSCI2-Test-MW1P.config /data:Config\common.txt /data:Config\prod.txt /data:Config\int.txt /data:Config\PXSCI2-Test-MW1P.txt"
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXSDEV1-Test-MW1P.config /data:Config\common.txt /data:Config\prod.txt /data:Config\int.txt /data:Config\PXSDEV1-Test-MW1P.txt"

    # Stress
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXS-Stress-MW1P.config /data:Config\common.txt /data:Config\prod.txt /data:Config\int.txt /data:Config\PXS-Stress-MW1P.txt"
    
    # Sandbox    
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.PXS-Sandbox-SN2.config /data:Config\common.txt /data:Config\prod.txt /data:Config\int.txt /data:Config\sandbox.txt"

    # Onebox
    Invoke-Expression "$genConfigPath /template:Config\Common.tpl /output:Config\Config.Onebox.config /data:Config\prod.txt /data:Config\int.txt /data:Config\onebox.txt"

    # Now, make the XML pretty.
    foreach ($child in (Get-ChildItem Config/*.config))
    {
        $xml = [xml](gc $child)
        $xml.Save($child.FullName)
    }

    $dllPath = $null
    foreach ($testPath in $packageLocations.Split(";"))
    {
        $test = [System.IO.Path]::Combine($testPath, "microsoft.windows.services.configgen\9.0.16068.1\lib\net45\Microsoft.Windows.Services.ConfigGen.dll")
        if (Test-Path $test)
        {
            $dllPath = $test
            break
        }
    }
    
    # Finally, codegen the c# file
    Add-Type -Path $dllPath
    $generator = New-Object -TypeName "Microsoft.Windows.Services.ConfigGen.ConfigGenerator" -ArgumentList @("Config\Config.PXS-PROD-BN3P.config")
    $cSharp = $generator.Generate();

    # Last, write the .CS file and the hash of the inputs
    $cSharp | Out-File .\Generated\CommonConfig.cs
    $hash | Out-File .\Generated\configstamp.txt

}
catch
{
    [System.Console]::WriteLine($_.Exception);
}

Pop-Location
[System.Environment]::CurrentDirectory = $oldLocation