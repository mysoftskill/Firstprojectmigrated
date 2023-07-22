import * as Pdms from "./pdms/pdms-types";

const CosmosStructuredStreamId = "CosmosStructuredStream";
const CosmosUnstructuredStreamId = "CosmosUnstructuredStream";

export class DataAssetHelper {
    //  Returns whether an asset group has no privacy actions.
    public static hasNoPrivacyActions(assetGroup: Pdms.AssetGroup): boolean {
        return !assetGroup.deleteAgentId && !assetGroup.exportAgentId;
    }

    //  Returns whether an asset group has any pending requests for privacy actions.
    public static hasPendingPrivacyActions(assetGroup: Pdms.AssetGroup): boolean {
        return !!(assetGroup.deleteSharingRequestId || assetGroup.exportSharingRequestId);
    }

    public static hasPrivacyActionsDetails(assetGroup: Pdms.AssetGroup): boolean {
        return DataAssetHelper.hasPendingPrivacyActions(assetGroup) || !DataAssetHelper.hasNoPrivacyActions(assetGroup);
    }

    //  Returns the privacy action IDs (non-localized) that the asset group supports.
    public static getPrivacyActionIds(assetGroup: Pdms.AssetGroup): string[] {
        let actions: string[] = [];

        if (assetGroup && assetGroup.deleteAgentId) {
            actions.push(Pdms.PrivacyActionId.Delete);
        }
        if (assetGroup && assetGroup.exportAgentId) {
            actions.push(Pdms.PrivacyActionId.Export);
        }

        return actions;
    }

    //  Returns the privacy action IDs (non-localized) that the asset group supports.
    public static getPendingPrivacyActionIds(assetGroup: Pdms.AssetGroup): string[] {
        let actions: string[] = [];

        if (assetGroup && assetGroup.deleteSharingRequestId) {
            actions.push(Pdms.PrivacyActionId.Delete);
        }
        if (assetGroup && assetGroup.exportSharingRequestId) {
            actions.push(Pdms.PrivacyActionId.Export);
        }

        return actions;
    }
    
    //  True if the asset group is one of the Cosmos asset types.
    public static isCosmosAssetGroup(assetGroup: Pdms.AssetGroup): boolean {
        return this.isCosmosAssetTypeId(assetGroup.qualifier.props.AssetType);
    }

    //  True if the asset type is one of the Cosmos asset types.
    public static isCosmosAssetType(assetType: Pdms.AssetType): boolean {
        return this.isCosmosAssetTypeId(assetType.id);
    }

    //  True if the asset type ID is one of the Cosmos asset type IDs.
    public static isCosmosAssetTypeId(assetTypeId: string): boolean {
        return this.isCosmosStructuredStreamAssetTypeId(assetTypeId) ||
            this.isCosmosUnstructuredStreamAssetTypeId(assetTypeId);
    }

    //  True if the asset type ID is CosmosStructuredStream.
    public static isCosmosStructuredStreamAssetTypeId(assetTypeId: string): boolean {
        return assetTypeId === CosmosStructuredStreamId;
    }

    //  True if the asset type ID is CosmosUnstructuredStream.
    public static isCosmosUnstructuredStreamAssetTypeId(assetTypeId: string): boolean {
        return assetTypeId === CosmosUnstructuredStreamId;
    }
}
