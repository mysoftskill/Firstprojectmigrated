# Readme

PrivacyCommandCustomActivity is a sample console application that is invoked by an ADF's custom activity. The application will then call PCF SDK to get batch commands from PCFv2, validate them, then save validated commands to a specified blob location for which agent will take actions on.

# Set up Example
Here is an exmaple how the infrastructure gets set up.

## Prerequisites:
- An Azure Data Factory Gen2 account
- An Azure Batch service account
- An Azure storage account
- Permission to access ADG.CS artifact store

## Download the PrivacyCommandCustomActivity package
- Run below commands to download the custom activity package from ADG.CS artifact store (answer yes if prompted to install the azure-devops extension). Update the version number to use the lastest package.
```
az login

az artifacts universal download --organization "https://msdata.visualstudio.com/" --project "15dc7165-47f6-4b30-8f7d-94f2091e7113" --scope project --feed "ADG.CS" --name "microsoft-privacyservices-commandfeed-customactivity" --version "1.6.1942" --path .
```
- Extract all files from downloaded PrivacyCommandCustomActivity.zip and upload them to an Azure storage container.

## ADF set up

1. Create a New pipeline, then New Activity. Under "Batch Service", add a custom activity
2. In the Azure Batch tab, connect to the Azure Batch service
3. In the Settings tab, 
   1) In **Command**, specify the executable to run, e.g. `PrivacyCommandCustomActivity.exe`, or `dotnet PrivacyCommandCustomActivity.dll`. Command line options:
   2) In **Folder**, specify the container name where the files from PrivacyCommandCustomActivity.zip were uploaded to.
   3) In **Resource Linked Service**, link to the storage account where the job run info will be stored.
   4) In **Extended Properties**, add the following properties that will be consumered by the custom activity: 
      - customActivityType
      - operation
      - agentId
      - assetGroupId
      - startTime
      - endTime
      - clientCertAKV
      - clientCertName
      - clientAadAppId
      - endpoint
      - outputBlobUrl
      - outputBlobContainerName
      - exportStagingContainerUri (only required for completing export commands)
      - exportStagingRootFolder (only required for completing export commands)
      - getAllCommands
      - maxResult

Below is a sample activity settings:

```
{
    "name": "pipeline1",
    "properties": {
        "activities": [
            {
                "name": "Custom1",
                "type": "Custom",
                "dependsOn": [],
                "policy": {
                    "timeout": "7.00:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "userProperties": [],
                "typeProperties": {
                    "command": "PcfCustomActivity.exe",
                    "resourceLinkedService": {
                        "referenceName": "AzureBlobStorage1",
                        "type": "LinkedServiceReference"
                    },
                    "folderPath": "pcfca",
                    "extendedProperties": {
                        "customActivityType": "GetCommands",
                        "operation": "Delete",
                        "agentId": "c852d9ca-81f0-4d01-b018-5ea061e9a349",
                        "assetGroupId": "18b06e10-2f6c-422d-b96e-4a9e69655f83",
                        "startTime": {
                            "value": "@pipeline().parameters.startTime",
                            "type": "Expression"
                        },
                        "endTime": {
                            "value": "@pipeline().parameters.endTime",
                            "type": "Expression"
                        },
                        "clientCertAKV": "https://sampleagentkv.vault.azure.net/",
                        "clientCertName": "sampleagentclient",
                        "clientAadAppId": "fb9f9d15-8fd7-4495-850f-8f5cb676555a",
                        "endpoint": "ppe",
                        "outputBlobUrl": "https://customactivityoutputs.blob.core.windows.net/",
                        "outputBlobContainerName": "output",
                        "getAllCommands": false,
                        "maxResult": 0
                    },
                    "referenceObjects": {
                        "linkedServices": [
                            {
                                "referenceName": "AzureBlobStorage1",
                                "type": "LinkedServiceReference"
                            }
                        ],
                        "datasets": []
                    }
                },
                "linkedServiceName": {
                    "referenceName": "kyleyuanbatch",
                    "type": "LinkedServiceReference"
                }
            }
        ],
        "parameters": {
            "startTime": {
                "type": "string",
                "defaultValue": "0"
            },
            "endTime": {
                "type": "string",
                "defaultValue": "0"
            }
        },
        "annotations": [],
        "lastPublishTime": "2022-02-27T17:47:54Z"
    },
    "type": "Microsoft.DataFactory/factories/pipelines"
}
```

## Azure Batch Set up

1. Create an Azure **Batch Service** Account
2. In the newly created batch account, go to **Pools**, select **+Add**
   - Identity: *Important* Assigned an UAMI which has the permissions to access keyvault and storage account specified in **Extended Properties**
   - Image Type: Marketplace
   - Publisher: microsoftwindowsserver
   - Offer: windowsserver
   - Sku: 2022-datacenter


## Upload build output

The build result from PrivacyCommandCustomActivity.csproj should be uploaded to the storage location specified in the custom activity set up. 

# Run custom activity

For testing purpose, the custom activity can be manually triggered at any time, just use **Add trigger"" then **Trigger Now**.

After the activity started, a new container **adfjobs** in the Azure Storage Account linked to the custom activity will be created, with each subfolder represnting a jobid, then a subfoler called **output** container the final stdin.txt and stdout.txt. Any output from custom activity to the console will be capture in stdout.txt for debugging purpose.

Also, before the job started, a folder named **runtime** will exist briefly until the end of the job run. This folder container the input parameters to the custom activity in 3 json files: activity.json, linkedServices.json, datasets.json

# Local testing

1. Copy activity.json under the test folder to the build output folder, e.g. src\PCF\bin\Debug\x64\PrivacyCommandCustomActivity
2. Update the extendedProperties section with the correct information you want to test with
3. Hit F5 in VS

# References:

- [Use custom activities in an Azure Data Factory or Azure Synapse Analytics pipeline](https://docs.microsoft.com/en-us/azure/data-factory/transform-data-using-custom-activity)
- [Creating An Azure Data Factory V2 Custom Activity](https://mrpaulandrew.com/2018/11/12/creating-an-azure-data-factory-v2-custom-activity/#:~:text=Create%20an%20ADF%20pipeline%20and%20with%20a%20vanilla%2clinked%20service%2c%20which%20is%20your%20Azure%20Blob%20Storage.)
