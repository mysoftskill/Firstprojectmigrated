# How to run Functional Tests locally

## Prerequisite

1. Local Git enlistment of Compliance Services repo 

2. Build: you can build solution from Visual Studio or How to build PCF/PXS/etc from cmdline 

3. Install the latest Azure Az Powershell modules: https://docs.microsoft.com/en-us/powershell/azure/install-az-ps

4. Devbox provisioning : 
	1. Run the Powershell setup script src\PCF\Product\Tools\ProvisionDevMachine.ps1.
	2. You may be prompted for your credentials (if this is your first time running the script) for credentials during RPS install.  Use your @microsoft.com account.


## Running PCF: 

From VS: open PCF solution in **elevated Admin Mode** and run target service in debug mode 
    1. PcfFrontdoor
    2. PcfWorker

From cmdline: start **elevated cmdline as an Admin** and chdir to the corresponding build output. 
    1. E.g. if your git enlistment is in D:\git\ComplianceServices then the debug build output will be here: D:\git\ComplianceServices\src\PCF\bin\Debug\x64\AutopilotRelease
    
        To start Frontdoor from cmdline: 

```
        cd to D:\git\ComplianceServices\src\PCF\bin\Debug\x64\AutopilotRelease\Frontdoor 
        .\Pcf.Frontdoor.exe
``` 

        Use the above approach for worker and other services 

 

## Running PCF Tests: 

FCT:
   1. Start Azure Storage Emulator
   2. Run the Frontdoor  and Worker processes. You can run those from VS or admin cmdline. Remember to run in **Admin mode**.

Unittests: you can run unittests from VS, admin mode is not required. 

Run your tests from test explorer.
 

## Debugging: 

1. Open PCF solution from VS in Admin mode 

2. Set breakpoints or simply start in debug mode required executable 

Another option is attach to already running process from elevated VS 

 

## Debugging in Azure Service Fabric: 

You can deploy it into local Service Fabric cluster and then attach to the process from elevated Visual Studio.