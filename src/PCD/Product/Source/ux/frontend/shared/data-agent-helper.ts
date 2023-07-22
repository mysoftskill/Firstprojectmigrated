import * as Pdms from "./pdms/pdms-types";
import { DataAssetHelper } from "./data-asset-helper";

export class DataAgentHelper {
    //  Returns a subset of asset groups which are linked to the agent ID via export.
    public static getAssetGroupsLinkedToExportAgent(exportAgentId: string, allAssetGroups: Pdms.AssetGroup[]): Pdms.AssetGroup[] {
        return _.filter(allAssetGroups, (ag: Pdms.AssetGroup) => ag.exportAgentId === exportAgentId);
    }

    //  Returns a subset of asset groups which are applicable to the provided protocol and have an action that is linkable to the provided agent.
    public static getLinkableAssetGroups(protocol: string, dataAgentId: string, allAssetGroups: Pdms.AssetGroup[]): Pdms.AssetGroup[] {
        let supportedActions = this.getSupportedAssetPrivacyActions(protocol);
        return _.filter(allAssetGroups, (ag: Pdms.AssetGroup) => {
            if (this.isLinkedAsset(ag, dataAgentId)) {
                return false;
            }
            return (supportedActions.deleteAction && !ag.deleteAgentId) || (supportedActions.exportAction && !ag.exportAgentId);
        });
    }

    //  Returns if an asset is currently linked to an agent.
    public static isLinkedAsset(assetGroup: Pdms.AssetGroup, dataAgentId: string): boolean {
        return assetGroup.deleteAgentId === dataAgentId || assetGroup.exportAgentId === dataAgentId;
    }

    //  Returns the privacy actions that describe which are supported by the protocol.
    public static getSupportedAssetPrivacyActions(protocol: string): Pdms.PrivacyActionsState {
        return {
            deleteAction: true,
            exportAction: true
        };
    }

    //  Returns true if the protocol is legacy (only if protocol is grandfathered in).
    public static isLegacyProtocol(protocol: string): boolean {
        return false;
    }

    //  Returns the protocol of a given data agent
    public static getProtocol(dataAgent: Pdms.DataAgent): string {
        let firstReleaseState = Object.keys(dataAgent.connectionDetails)[0];
        return dataAgent.connectionDetails[firstReleaseState].protocol;
    }

    //  Determines if the given protocol is cosmos or not
    public static isCosmosProtocol(protocol: string): boolean {
        return protocol === Pdms.PrivacyProtocolId.CosmosDeleteSignalV2;
    }
}
