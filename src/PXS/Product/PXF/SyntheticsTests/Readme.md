# Running locally

For more details go here: <https://eng.ms/docs/products/geneva/runners/synthetics/testing-localbox>

## Download RunSynthetics tool

```code
NuGet.exe Install Geneva.Synthetics.Tools.RunSynthetics -source <https://msazure.pkgs.visualstudio.com/_packaging/synthetics/nuget/v3/index.json>
```

You will need to edit the RunSynthetics.exe.config to add binding redirects.

- Add the binding redirects from your {Name of SyntheticsTest dll}.dll.config to the top of the RunSynthetics.exe.config
- Remove any duplicates.
- If your app uses a different version than RunSynthetics, then you may have to edit the version info

  - For example, RunSynthetics uses a differnt version of the Microsoft.Extensions.Logging.Abstraction (2.1.1.0) and Newtonsoft (12.0.0.0) libraries

```xml
  <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
   <dependentAssembly>
     <assemblyIdentity name="Microsoft.Extensions.Logging.Abstractions" publicKeyToken="adb9793829ddae60" culture="neutral" />
     <bindingRedirect oldVersion="0.0.0.0-3.1.18.0" newVersion="2.1.1.0" />
   </dependentAssembly>
  </assemblyBinding>
  <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
    <dependentAssembly>
      <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="12.0.0.0" />
    </dependentAssembly>
  </assemblyBinding>
```

- Ensure that MessagePack version is 1.9.11

## Run the synthetics tools against your local project

RunSynthetics.exe -a <"Location where the binaries are present"> -c <"Location of the config file"> -j <"The Job Name that you want to test"> -i <"Instance of the job name"> --debug true

E.g.,

```code
RunSynthetics\RunSynthetics.exe -a D:\git\SyntheticsSample\bin\x64\Debug\net472\ -c D:\git\SyntheticsSample\bin\x64\Debug\net472\GraphApiSyntheticsJobGroup.json  -j ExportPersonalDataSyntheticsJob -i IN1 --debug true
```

### Output

```code
Attach debugger to process name: RunSynthetics and Id: 57068
```

Attach to the RunSynthetics.exe process (Debug -> Attach to Process… ) in a Visual Studio session (must be running in Admin mode).

# Deploying from you devbox:

To deploy a job from your local box, you can use GenevaSyntheticsClient. Details can be found at: https://eng.ms/docs/products/geneva/runners/synthetics/syntheticsclientreference

For example, to deploy ExportPersonalData synthetcis jobs, you can use the separate ExportPersonalDataSyntheticsJobGroup.json file: 

GenevaSyntheticsClient\GenevaSyntheticsClient.exe  deploy --environment prod --account adgcsprod --jobgroup ExportPersonalDataSyntheticsJobGroup --package D:\git\ComplianceServices\src\PXS\Bin\Debug\x64\ExportPersonalDataSyntheticsJob --config D:\git\ComplianceServices\src\PXS\Bin\Debug\x64\ExportPersonalDataSyntheticsJob\ExportPersonalDataSyntheticsJobGroup.json --checkstatus

## When done, clean up test jobs
	
### List jobs:

```code
GenevaSyntheticsClient.exe  list --environment prod --account adgcsprod -j exportpersonaldatasyntheticsjobgroup
```

### Delete Jobs:

```code
GenevaSyntheticsClient.exe  delete --environment prod --account adgcsprod -j exportpersonaldatasyntheticsjobgroup
```

Once you are sure that your changes work, make sure to update SyntheticsJobGroup.json on the Common project, as that file us used during deployment to deploy all 
of the synthetic jobs.

