### ADX Cluster buildout using Ev2 templates
AdgcsGeneva ADX cluster and databases were created using Ev2 templates as described at
 [Learn more](https://kusto.azurewebsites.net/docs/kusto/ops/ev2-buildout.html)

Some of the parameters used in ScopeBindings.json are described as the following.

	1. To interact with cluster through the Kusto Ev2 Extension we need to create an application. The app created can be found at [adgcs-geneva-ev2-app - Microsoft Azure](https://ms.portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/a974e85e-8a08-4f74-bfbc-7284218bc5ab/isMSAApp/), and its id is "a974e85e-8a08-4f74-bfbc-7284218bc5ab" which is defined as the "__APPID__" parameter.

	2. The application created above needs a certificate to communicate to Kusto and certificate path is located in the "__CERTIFICATEPATH__" parameter and its value is https://adg-cs-prod-kv.vault.azure.net/secrets/geneva-kusto.
	
	3. "__TENANTID__" parameter value is 33e01921-4d64-4f8c-a055-5bdaffd5e33d which is our AME tenant.

	4. Parameter "__SUBID__" is the subscription Id used in PROD environment which is "4e261d67-9395-4cec-a519-14053c4765e3".

	5. A new resource group was created for the Geneva to Kusto work, its name is "ADG-CS-GENEVA-KUSTO-RG" as defined as "__RESOURCEGROUP__" parameter.

### How to rollout
As described at the bottom of the Kusto documenation page above, using Ev2 powerwshell commandlet to launch the rollout.

New-AzureServiceRollout -ServiceGroupRoot C:\path-to-the-serviceGroup-folder -RolloutSpec RolloutSpec.json -RolloutInfra Prod -WaitToComplete

The rollout commandlet above will create the Kusto cluster and databases defined in the ARM template. Since the AdgcsGeneva cluster and databases have already been created using this commandlet, there's no need to run it again.

### Notes

KustoCommandCreateFunction.Extension.RolloutParameters.json and KustoCommandCreateTable.Extension.RolloutParameters.json are not used for now, 
because only cluster and databases are created for our services and extra tables and functions are not needed. They are included as examples in case we need to create them later.
