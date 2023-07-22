import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { DataAssetHelper } from "./data-asset-helper";
import * as Pdms from "./pdms/pdms-types";

describe("Data asset helper", () => {
    let spec: TestSpec;
    let assetGroup: Pdms.AssetGroup;

    beforeEach(() => {
        spec = new TestSpec();

        assetGroup = {
            "id": "GUID-555",
            "deleteAgentId": null,
            "exportAgentId": null,
            "ownerId": "GUID-888",
            "qualifier": {
                "props": {
                    "AssetType": "CosmosStructuredStream",
                    "PhysicalCluster": "ExamplePhysicalCluster",
                    "VirtualCluster": "ExampleVirtualCluster",
                    "RelativePath": "/local/path"
                }
            }
        };
    });

    describe("hasNoPrivacyActions", () => {
        it("returns false for delete and export agents", () => {
            assetGroup.deleteAgentId = "anyAgentId";
            assetGroup.exportAgentId = "anyAgentId";

            expect(DataAssetHelper.hasNoPrivacyActions(assetGroup)).toBe(false);
        });
        it("returns false for delete agents", () => {
            assetGroup.deleteAgentId = "anyAgentId";

            expect(DataAssetHelper.hasNoPrivacyActions(assetGroup)).toBe(false);
        });
        it("returns false for export agents", () => {
            assetGroup.exportAgentId = "anyAgentId";

            expect(DataAssetHelper.hasNoPrivacyActions(assetGroup)).toBe(false);
        });
    });

    describe("getPrivacyActionIds", () => {
        it("returns delete and export agents", () => {
            assetGroup.deleteAgentId = "anyAgentId";
            assetGroup.exportAgentId = "anyAgentId";

            expect(DataAssetHelper.getPrivacyActionIds(assetGroup))
                .toEqual([Pdms.PrivacyActionId.Delete, Pdms.PrivacyActionId.Export]);
        });
        it("returns for delete agents", () => {
            assetGroup.deleteAgentId = "anyAgentId";

            expect(DataAssetHelper.getPrivacyActionIds(assetGroup))
                .toEqual([Pdms.PrivacyActionId.Delete]);
        });
        it("returns for export agents", () => {
            assetGroup.exportAgentId = "anyAgentId";

            expect(DataAssetHelper.getPrivacyActionIds(assetGroup))
                .toEqual([Pdms.PrivacyActionId.Export]);
        });

        it("returns nothing for no linked agents", () => {
            assetGroup.deleteAgentId = "";
            assetGroup.exportAgentId = "";

            expect(DataAssetHelper.getPrivacyActionIds(null)).toEqual([]);
        });

        it("returns nothing for no asset group", () => {
            expect(DataAssetHelper.getPrivacyActionIds(null)).toEqual([]);
        });
    });

    describe("getPendingPrivacyActionIds", () => {
        it("returns delete and export agents", () => {
            assetGroup.deleteSharingRequestId = "anyAgentId";
            assetGroup.exportSharingRequestId = "anyAgentId";

            expect(DataAssetHelper.getPendingPrivacyActionIds(assetGroup))
                .toEqual([Pdms.PrivacyActionId.Delete, Pdms.PrivacyActionId.Export]);
            expect(DataAssetHelper.hasPendingPrivacyActions(assetGroup)).toEqual(true);
        });
        it("returns for delete agents", () => {
            assetGroup.deleteSharingRequestId = "anyAgentId";

            expect(DataAssetHelper.getPendingPrivacyActionIds(assetGroup))
                .toEqual([Pdms.PrivacyActionId.Delete]);
            expect(DataAssetHelper.hasPendingPrivacyActions(assetGroup)).toEqual(true);
        });
        it("returns for export agents", () => {
            assetGroup.exportSharingRequestId = "anyAgentId";

            expect(DataAssetHelper.getPendingPrivacyActionIds(assetGroup))
                .toEqual([Pdms.PrivacyActionId.Export]);
            expect(DataAssetHelper.hasPendingPrivacyActions(assetGroup)).toEqual(true);
        });

        it("returns nothing for no linked agents", () => {
            assetGroup.deleteSharingRequestId = "";
            assetGroup.exportSharingRequestId = "";

            expect(DataAssetHelper.getPendingPrivacyActionIds(null)).toEqual([]);
            expect(DataAssetHelper.hasPendingPrivacyActions(assetGroup)).toEqual(false);
        });

        it("returns nothing for no asset group", () => {
            expect(DataAssetHelper.getPendingPrivacyActionIds(null)).toEqual([]);
            expect(DataAssetHelper.hasPendingPrivacyActions(assetGroup)).toEqual(false);
        });
    });

    describe("Cosmos asset type detection", () => {
        it("validates asset group", () => {
            assetGroup.qualifier.props.AssetType = "CosmosStructuredStream";
            expect(DataAssetHelper.isCosmosAssetGroup(assetGroup)).toBe(true);

            assetGroup.qualifier.props.AssetType = "CosmosUnstructuredStream";
            expect(DataAssetHelper.isCosmosAssetGroup(assetGroup)).toBe(true);
        });

        it("validates asset type", () => {
            let assetType: Pdms.AssetType = {
                "id": "CosmosStructuredStream",
                "label": "whatever",
                "props": [{
                    "id": "PhysicalCluster",
                    "label": "whatever",
                    "description": "whatever",
                    "required": true
                }, {
                    "id": "VirtualCluster",
                    "label": "whatever",
                    "description": "whatever",
                    "required": true
                }, {
                    "id": "RelativePath",
                    "label": "whatever",
                    "description": "whatever",
                    "required": false
                }]
            };

            assetType["id"] = "CosmosStructuredStream";
            expect(DataAssetHelper.isCosmosAssetType(assetType)).toBe(true);

            assetType["id"] = "CosmosUnstructuredStream";
            expect(DataAssetHelper.isCosmosAssetType(assetType)).toBe(true);
        });

        it("validates asset type ID", () => {
            expect(DataAssetHelper.isCosmosAssetTypeId("CosmosStructuredStream")).toBe(true);
            expect(DataAssetHelper.isCosmosAssetTypeId("CosmosUnstructuredStream")).toBe(true);
        });
    });
});
