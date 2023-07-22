import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { DataAgentHelper } from "./data-agent-helper";
import * as Pdms from "./pdms/pdms-types";

let sampleDataAgent: Pdms.DataAgent;
let sampleAssetGroups: Pdms.AssetGroup[];
let sampleAssetTypes: Pdms.AssetType[];

describe("Data agent helper", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();

        resetSamples();
    });

    describe("getAssetGroupsLinkedToExportAgent", () => {
        it("returns only assets with the specific export agent", () => {
            sampleAssetGroups[0].exportAgentId = "anyExportAgentId";
            sampleAssetGroups[1].exportAgentId = "differentExportAgentId";
            let assetGroups = DataAgentHelper.getAssetGroupsLinkedToExportAgent("anyExportAgentId", sampleAssetGroups);

            expect(_.every(assetGroups, (ag: Pdms.AssetGroup) => !!ag.exportAgentId)).toBe(true);
            expect(assetGroups.length).toBe(1);
            expect(assetGroups[0].exportAgentId).toBe("anyExportAgentId");
        });

        it("returns an empty array if there are no assets with the export agent", () => {
            expect(DataAgentHelper.getAssetGroupsLinkedToExportAgent("uniqueExportAgentId", sampleAssetGroups)).toEqual([]);
        });
    });

    describe("getLinkableAssetGroups", () => {
        it("returns only assets without data agents", () => {
            sampleAssetGroups[0].deleteAgentId = "anyDeleteAgentId";
            sampleAssetGroups[0].exportAgentId = "anyExportAgentId";
            sampleAssetGroups[1].deleteAgentId = null;
            sampleAssetGroups[1].exportAgentId = null;
            let assetGroups = DataAgentHelper.getLinkableAssetGroups("CosmosDeleteSignalV2", "anyAgentId", sampleAssetGroups);

            expect(_.every(assetGroups, (ag: Pdms.AssetGroup) => !ag.deleteAgentId)).toBe(true);
            expect(_.every(assetGroups, (ag: Pdms.AssetGroup) => !ag.exportAgentId)).toBe(true);
        });

        it("returns only assets that can be linked", () => {
            sampleAssetGroups[0].deleteAgentId = "anyDeleteAgentId";
            sampleAssetGroups[0].exportAgentId = null;
            sampleAssetGroups[1].deleteAgentId = "anyOtherDeleteAgentId";
            sampleAssetGroups[1].exportAgentId = null;
            let assetGroups = DataAgentHelper.getLinkableAssetGroups("CommandFeedV1", "anyAgentId", sampleAssetGroups);

            expect(assetGroups.length).toBe(2);
        });

        it("returns an empty array if there are no assets without data agents", () => {
            _.forEach(sampleAssetGroups, (ag: Pdms.AssetGroup) => ag.deleteAgentId = "anyDeleteAgentId");
            _.forEach(sampleAssetGroups, (ag: Pdms.AssetGroup) => ag.exportAgentId = "anyExportAgentId");

            expect(DataAgentHelper.getLinkableAssetGroups("anyProtocol", "anyAgentId", sampleAssetGroups)).toEqual([]);
        });
    });

    describe("isLinkedAsset", () => {
        it("returns true if delete agent matches", () => {
            sampleAssetGroups[0].deleteAgentId = "anyDeleteAgentId";
            sampleAssetGroups[0].exportAgentId = "";

            expect(DataAgentHelper.isLinkedAsset(sampleAssetGroups[0], "anyDeleteAgentId")).toBe(true);
        });

        it("returns true if export agent matches", () => {
            sampleAssetGroups[0].deleteAgentId = "";
            sampleAssetGroups[0].exportAgentId = "anyExportAgentId";

            expect(DataAgentHelper.isLinkedAsset(sampleAssetGroups[0], "anyExportAgentId")).toBe(true);
        });

        it("returns true if all agent ids match", () => {
            sampleAssetGroups[0].deleteAgentId = "anyAgentId";
            sampleAssetGroups[0].exportAgentId = "anyAgentId";

            expect(DataAgentHelper.isLinkedAsset(sampleAssetGroups[0], "anyAgentId")).toBe(true);
        });

        it("returns true if one agent ids match", () => {
            sampleAssetGroups[0].deleteAgentId = "anyAgentId";
            sampleAssetGroups[0].exportAgentId = "anyOtherAgentId";

            expect(DataAgentHelper.isLinkedAsset(sampleAssetGroups[0], "anyAgentId")).toBe(true);
        });

        it("returns false if no agent ids are specified on the asset", () => {
            sampleAssetGroups[0].deleteAgentId = "";
            sampleAssetGroups[0].exportAgentId = "";

            expect(DataAgentHelper.isLinkedAsset(sampleAssetGroups[0], "anyAgentId")).toBe(false);
        });

        it("returns false if no agent ids match", () => {
            sampleAssetGroups[0].deleteAgentId = "anyDeleteAgentId";
            sampleAssetGroups[0].exportAgentId = "anyExportAgentId";

            expect(DataAgentHelper.isLinkedAsset(sampleAssetGroups[0], "anyAgentId")).toBe(false);
        });
    });

    describe("getSupportedAssetPrivacyActions", () => {
        it("returns true for any protocol supporting any privacy action", () => {
            expect(DataAgentHelper.getSupportedAssetPrivacyActions("anyProtocol")).toEqual({ deleteAction: true, exportAction: true });
        });
    });

    describe("isLegacyProtocol", () => {
        it("returns true for legacy protocols", () => {
            let legacyProtocols: string[] = [
            ];

            _.forEach(legacyProtocols, (protocol: string) => {
                expect(DataAgentHelper.isLegacyProtocol(protocol)).toBe(true);
            });
        });

        it("returns false for actively supported protocols", () => {
            let activeProtocols: string[] = [
                "CosmosDeleteSignalV2",
                "CommandFeedV1"
            ];

            _.forEach(activeProtocols, (protocol: string) => {
                expect(DataAgentHelper.isLegacyProtocol(protocol)).toBe(false);
            });
        });
    });

    describe("getProtocol", () => {
        it("returns the protocol of the agent", () => {
            expect(DataAgentHelper.getProtocol(sampleDataAgent)).toBe("CosmosDeleteSignalV2");
        });

        it("assumes protocols are consistent across release states", () => {
            sampleDataAgent.connectionDetails["RS1"] = {
                "protocol": "DifferentProtocolExample",
                "releaseState": "RS1"
            };

            expect(DataAgentHelper.getProtocol(sampleDataAgent)).toBe("CosmosDeleteSignalV2");
        });
    });

    describe("isCosmosProtocol", () => {
        it("returns true when the protocol is a cosmos protocol", () => {
            expect(DataAgentHelper.isCosmosProtocol(Pdms.PrivacyProtocolId.CosmosDeleteSignalV2)).toBe(true);
        });

        it("returns false when the protocol is not a cosmos protocol", () => {
            expect(DataAgentHelper.isCosmosProtocol(Pdms.PrivacyProtocolId.CommandFeedV1)).toBe(false);
        });
    });
});

function resetSamples() {
    sampleDataAgent = {
        kind: "delete-agent",
        assetGroups: null,
        id: "GUID-555",
        name: "SampleDataAgent",
        description: "agent",
        ownerId: "GUID-888",
        sharingEnabled: false,
        isThirdPartyAgent: false,
        hasSharingRequests: false,
        connectionDetails: {
            PreProd: {
                "protocol": "CosmosDeleteSignalV2",
                "releaseState": "PreProd"
            }
        },
        operationalReadiness: null,
        deploymentLocation: null,
        supportedClouds: null,
        pendingCommandsFound: false,
        dataResidencyBoundary: "Global"
    };

    sampleAssetGroups = [
        {
            "id": "GUID-555",
            "deleteAgentId": null,
            "ownerId": "GUID-888",
            "qualifier": {
                "props": {
                    "AssetType": "CosmosStructuredStream",
                    "PhysicalCluster": "ExamplePhysicalCluster",
                    "VirtualCluster": "ExampleVirtualCluster",
                    "RelativePath": "/local/path"
                }
            }
        }, {
            "id": "GUID-545",
            "deleteAgentId": null,
            "ownerId": "GUID-888",
            "qualifier": {
                "props": {
                    "AssetType": "File",
                    "ServerPath": "ExampleServer",
                    "FileName": "File-y.txt"
                }
            }
        }
    ];

    sampleAssetTypes = [
        {
            "id": "API",
            "label": "API",
            "props": [{
                "id": "Host",
                "label": "Host",
                "description": "The host name and scheme of the API.",
                "required": true
            }, {
                "id": "Path",
                "label": "Path",
                "description": "The Path to the API .",
            }, {
                "id": "Method",
                "label": "Method",
                "description": "The allowed Method for the API .",
            }]
        }, {
            "id": "ApplicationService",
            "label": "Application Service",
            "props": [{
                "id": "Host",
                "label": "Host",
                "description": "The DNS host name of the service URL.",
                "required": true
            }, {
                "id": "Path",
                "label": "Path",
                "description": "The substring after host name of the service URL.",
                "required": false
            }]
        }, {
            "id": "AzureBlob",
            "label": "Azure Blob",
            "props": [{
                "id": "AccountName",
                "label": "Account Name",
                "description": "The storage account name.",
                "required": true
            }, {
                "id": "ContainerName",
                "label": "Container Name",
                "description": "The container name.",
                "required": false
            }, {
                "id": "BlobPattern",
                "label": "Blob Pattern",
                "description": "The pattern for identifying the blobs.",
                "required": false
            }]
        }, {
            "id": "AzureDocumentDB",
            "label": "Azure Document DB",
            "props": [{
                "id": "AccountName",
                "label": "Account Name",
                "description": "The storage account name.",
                "required": true
            }, {
                "id": "DatabaseName",
                "label": "Database Name",
                "description": "The database name.",
                "required": false
            }, {
                "id": "CollectionName",
                "label": "Collection Name",
                "description": "The collection name.",
                "required": false
            }]
        }, {
            "id": "AzureSql",
            "label": "Azure Sql",
            "props": [{
                "id": "ServerName",
                "label": "Server Name",
                "description": "The sql server name.",
                "required": true
            }, {
                "id": "DatabaseName",
                "label": "Database Name",
                "description": "The database name.",
                "required": false
            }, {
                "id": "TableName",
                "label": "Table Name",
                "description": "The table name.",
                "required": false
            }]
        }, {
            "id": "AzureTable",
            "label": "Azure Table",
            "props": [{
                "id": "AccountName",
                "label": "Account Name",
                "description": "The storage account name.",
                "required": true
            }, {
                "id": "TableName",
                "label": "Table Name",
                "description": "The table name.",
                "required": false
            }]
        }, {
            "id": "CosmosStructuredStream",
            "label": "Cosmos Structured Stream",
            "props": [{
                "id": "PhysicalCluster",
                "label": "Physical Cluster",
                "description": "The physical cluster name.",
                "required": true
            }, {
                "id": "VirtualCluster",
                "label": "Virtual Cluster",
                "description": "The virtual cluster name.",
                "required": true
            }, {
                "id": "RelativePath",
                "label": "Relative Path",
                "description": "The relative path to the stream.",
                "required": false
            }]
        }, {
            "id": "CosmosUnstructuredStream",
            "label": "Cosmos Unstructured Stream",
            "props": [{
                "id": "PhysicalCluster",
                "label": "Physical Cluster",
                "description": "The physical cluster name.",
                "required": true
            }, {
                "id": "VirtualCluster",
                "label": "Virtual Cluster",
                "description": "The virtual cluster name.",
                "required": true
            }, {
                "id": "RelativePath",
                "label": "Relative Path",
                "description": "The relative path to the stream.",
                "required": false
            }]
        }, {
            "id": "File",
            "label": "File",
            "props": [{
                "id": "ServerPath",
                "label": "Server Path",
                "description": "The path to the file using a fully qualifier server name.",
                "required": true
            }, {
                "id": "FileName",
                "label": "File Name",
                "description": "The file name.",
                "required": false
            }]
        }, {
            "id": "Kusto",
            "label": "Kusto",
            "props": [{
                "id": "ClusterName",
                "label": "Cluster Name",
                "description": "The Kusto cluster name.",
                "required": true
            }, {
                "id": "DatabaseName",
                "label": "Database Name",
                "description": "The database name.",
                "required": false
            }, {
                "id": "TableName",
                "label": "Table Name",
                "description": "The table name.",
                "required": false
            }]
        }, {
            "id": "PlatformService",
            "label": "Platform Service",
            "props": [{
                "id": "Host",
                "label": "Host",
                "description": "The DNS host name of the service URL.",
                "required": true
            }, {
                "id": "Path",
                "label": "Path",
                "description": "The substring after host name of the service URL.",
                "required": false
            }]
        }, {
            "id": "SqlServer",
            "label": "Sql Server",
            "props": [{
                "id": "ServerName",
                "label": "Server Name",
                "description": "The sql server name.",
                "required": true
            }, {
                "id": "DatabaseName",
                "label": "Database Name",
                "description": "The database name.",
                "required": false
            }, {
                "id": "TableName",
                "label": "Table Name",
                "description": "The table name.",
                "required": false
            }]
        }];
}
