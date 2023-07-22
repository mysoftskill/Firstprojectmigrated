Warning: 
Do not modify these scripts (unless you know what you are doing). If there is a bug in the scripts, we should contact the source for which these originated, which is Vortex.

Vortex Git location sourced from:
https://microsoft.visualstudio.com/DefaultCollection/e8efa521-db8e-4531-9cd8-6923807c7e83/_git/d53604d5-7539-40ba-b332-6fe4024c7e55?path=%2FSrc%2FBuild&version=GBmaster

Files used:
Collect-CatalogFiles.ps1
CreateCatalogFiles.cmd
Delete-EmptyFiles.ps1
Update-CatalogFiles.ps1

In addition, we have published a nuget package in the MEE.Privacy feed containing required compiled binaries involved in creation of catalog files. There is an additional readme in that package explaining where those binaries came from. The package name can be found in the packages.config.

Build configuration:

1. Modify your Build to restore these packages during the NuGet restore stage of your build.

  <package id="Microsoft.PrivacyServices.CodeSign" version="9.0.17262.15" />
  <package id="Microsoft.PrivacyServices.CodeSign.Dependencies" version="1.0.0.0" />


2. In your Build Definition (that needs code signing), add the following task group between these tasks below:
    a. Copy files to $(build.artifactstagingdirectory)
    b. Task group: MEE PrivacyServices CodeSign
    c. Publish Artifact Drop: $(build.artifactstagingdirectory)

An example of the above is found at:
https://microsoft.visualstudio.com/DefaultCollection/Universal%20Store/Universal%20Store%20Team/_apps/hub/ms.vss-ciworkflow.build-ci-hub?_a=edit-build-definition&id=9369