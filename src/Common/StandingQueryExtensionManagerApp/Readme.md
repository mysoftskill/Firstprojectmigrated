This is a fake app to build StandingQueryExtensionManagerApp.
When you build the app it will:
- restore StandingQueryExtensionManagerInstall nuget package
- replace ApplicationManifest.xml in StandingQueryExtensionManagerApp.sfpkg with modified one.
- place updated StandingQueryExtensionManagerApp.sfpkg into src/Deployment/Bin folder

Modified ApplicationManifest.xml has a new parameter: PlacementConstraints to specify on which ServiceFabric node type (monitoring role) the StandingQueryExtensionManagerApp will be running.
This is a workaround to have multiply monitoring roles in the same cluster.

StandingQueryExtensionManagerInstall nuget package contains Service Fabric standing query extension App: StandingQueryExtensionManagerApp.sfpkg