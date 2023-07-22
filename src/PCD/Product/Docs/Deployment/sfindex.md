# Service Fabric Deployment

## Glossary

1. SF - Service Fabric
1. ATM - Azure Traffic Manager
1. DNS - Domain Name Service

## Environments

1. Devbox - local environment.
1. PPE (PDMS-PPE DNS) - pre-production environment. Points to all production
   environments, but still uses test AAD app ID.
1. PROD (PDMS-Prod DNS) - production environment. Points to all production
   environments, uses production AAD app ID.

## VSTS

1. [CI build definition][CI build]: ran on each PR change; unit tests are executed;
   deployment package is also prepared
1. [PPE release definition][PPE release]: ran on every successful check in to `develop' or can be triggered manually from any other branch; code is
   signed; integration tests are executed within CloudTest; deployment package is released; INT environment deployment can be manually triggered
1. [Prod Release][Prod release]: ran on every successful check in to `master`; code is
   signed; integration tests are executed with CloudTest; deployment package is prepared.

### Service Fabric Application Build
The Service Fabric application combines the other built projects into a single package.
The package is a combination of:

1. CertInstaller
2. ServiceFabricIni
3. UX app
4. wwwroot
5. appsettings

The built artifact is completely packaged within the Service Fabric container. 

The details of this can be found in [SF Project File][Sfproj File]

### Test Deployment locally

1. Build Service Fabric application
2. Start Service Fabric Cluster

![Starting Service Fabric Cluster](images/start_sf_cluster.png)

3. Right click - Publish

![Publishing to SF](images/publish_to_sf.png)

4. Make sure that the connection endpoint is Local

![Connection Endpoint](images/connection_endpoint_local_cluster.png)


There are several ways to debug the SF Deployment:
1. Use Service Fabric Explorer
1. Check C:/SFDevCluster/Data/_App/_Node_0 for logs



## Service Fabric Startup layout

The Startup procedure is described in the Application and Service Manifests [Service Manifest][Service Manifest].

1. The SF launchs a setup script
2. The ux application launches

## Per-environment configurations

#### Environment Variables

1. Environment variables are initially set using an ARM template
2. These variables are then passed to the SF Application Manifest
3. These are processed by the flatteners


#### Flattener
Autopilot used to apply a flattener to the config files. That is why there are 2 types of flattening occuring. 

The types of flattener are: 
1. IniFlattener used for config-certsubjectname.ini
2. Flattener used for the appconfig folder, which is based on appsettings.json and [Startup Base][Flattener Code]

Using `Product\Source\ux\web.config` has per-environment configuration for ASP.NET
Core variables.

## OneBranch

The build pipeline is controlled by using yml files.  These files are flattened and applied based on the pipeline name. 

The yml files reference starter scripts in OneBranch/Build, which then references files in [Product/Build][Product Build Scripts]

YML Documentation: [OneBranch Documentation][OneBranch Documentation]

## CloudTest

There a two types of tests run on CloudTest: 
1. [Integration Tests][Integration Test] (previously known as i9n tests)
2. [KeepAlive (AKA Smoke Test)][Smoke Test]

CloudTest Documentation: [CloudTest Documentation]

### Troubleshooting tips

Check that certs were properly installed by CertInstaller

If slllogs_X and pdmsux\_X.log are present, the service is running.

Check IPs, DNS, ports.

[CI build]: https://msdata.visualstudio.com/ADG_Compliance_Services/_build?definitionId=7531&_a=summary
[PPE release]: https://msdata.visualstudio.com/ADG_Compliance_Services/_release?_a=releases&view=mine&definitionId=12
[Prod release]: https://msdata.visualstudio.com/ADG_Compliance_Services/_release?definitionId=15&view=mine&_a=releases
[Sfproj File]: https://msdata.visualstudio.com/ADG_Compliance_Services/_git/ComplianceServices?path=%2Fsrc%2FPCD%2FProduct%2FAzure%2FPcdUxApp%2FPcdUxApp.sfproj&version=GBdevelop&line=58&lineEnd=75&lineStartColumn=5&lineEndColumn=1&lineStyle=plain
[Service Manifest]: https://msdata.visualstudio.com/ADG_Compliance_Services/_git/ComplianceServices?path=%2Fsrc%2FPCD%2FProduct%2FAzure%2FPcdUxApp%2FApplicationPackageRoot%2FPcdUxSvcPkg%2FServiceManifest.xml&version=GBdevelop&_a=contents
[Flattener Code]: https://msdata.visualstudio.com/ADG_Compliance_Services/_git/ComplianceServices?path=%2Fsrc%2FPCD%2FProduct%2FSource%2Fux%2FStartup%2FStartupBase.cs&version=GBdevelop&line=268&lineEnd=268&lineStartColumn=1&lineEndColumn=68&lineStyle=plain
[Integration Test]: https://msdata.visualstudio.com/ADG_Compliance_Services/_git/ComplianceServices?path=%2Fsrc%2FPCD%2FProduct%2FSource%2Fux%2FCloudTest&version=GBdevelop&_a=contents
[Smoke Test]: https://msdata.visualstudio.com/ADG_Compliance_Services/_git/ComplianceServices?path=%2Fsrc%2FPCD%2FProduct%2FSource%2Ffunctional.tests&version=GBdevelop&_a=contents
[Product Build Scripts]: https://msdata.visualstudio.com/ADG_Compliance_Services/_git/ComplianceServices?path=%2Fsrc%2FPCD%2FProduct%2FBuild&version=GBdevelop&_a=contents
[OneBranch Documentation]: https://onebranch.visualstudio.com/Pipeline/_wiki/wikis/Pipeline.wiki/232/CDPx-Cross-Platform-Cloud-Delivery-Pipeline
[CloudTest Documentation]: https://1esdocs.azurewebsites.net/test/CloudTest/What-Is-CloudTest.html
