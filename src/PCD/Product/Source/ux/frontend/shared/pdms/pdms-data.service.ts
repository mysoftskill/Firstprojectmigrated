import { Service, Inject } from "../../module/app.module";

import * as Pdms from "../pdms/pdms-types";
import * as SharedTypes from "../shared-types";
import * as SearchTypes from "../search-types";
import { DataAgentHelper } from "../data-agent-helper";
import { IPdmsApiService } from "./pdms-api.service";
import { SetAgentRelationshipRequest } from "../pdms-agent-relationship-types";

/**
 *  Indicates that entity has a unique ID. 
 **/
interface IEntityWithId {
    //  Unique ID.
    id: string;
}

//  A shortcut for PDMS API method signature.
type PdmsApiEntityMethod<T extends IEntityWithId> = (entity: T) => ng.IHttpPromise<any>;

@Service({
    name: "pdmsDataService"
})
@Inject("$q", "pdmsApiService")
class PdmsDataCacheService implements Pdms.IPdmsDataService {
    private owners: Pdms.DataOwner[];
    private privacyPolicy: Pdms.PrivacyPolicy;

    constructor(
        private $promises: ng.IQService,
        private pdmsApiService: IPdmsApiService) {
    }

    public getCountriesList(): ng.IPromise<Pdms.Country[]> {
        return this.pdmsApiService.getCountriesList().then((response: ng.IHttpPromiseCallbackArg<Pdms.Country[]>) => {
            return response.data;
        });
    }

    public getPrivacyPolicy(): ng.IPromise<Pdms.PrivacyPolicy> {
        if (this.privacyPolicy) {
            return this.$promises.resolve(this.privacyPolicy);
        }

        return this.pdmsApiService.getPrivacyPolicy().then((response: ng.IHttpPromiseCallbackArg<Pdms.PrivacyPolicy>) => {
            return this.privacyPolicy = response.data;
        });
    }

    public updateAssetGroup(assetGroup: Pdms.AssetGroup): ng.IPromise<Pdms.AssetGroup> {
        let operation = this.createOrUpdate(assetGroup, this.pdmsApiService.createAssetGroup, this.pdmsApiService.updateAssetGroup);

        return operation.then((response: ng.IHttpPromiseCallbackArg<Pdms.AssetGroup>) => {
            return response.data;
        });
    }

    public createNewDataAgentInstance(kind: Pdms.DataAgentKind): Pdms.DataAgent {
        const preProdState = Pdms.ReleaseState[Pdms.ReleaseState.PreProd];

        switch (kind) {
            case "delete-agent":
                let connectionDetails: Pdms.DataAgentConnectionDetailsGroup = {};
                connectionDetails[preProdState] = <Pdms.DataAgentConnectionDetails> {
                    protocol: Pdms.PrivacyProtocolId.CommandFeedV2Batch,
                    authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth],
                    releaseState: preProdState,
                    agentReadiness: Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd]
                };

                return {
                    kind: "delete-agent",
                    id: "",
                    ownerId: "",
                    sharingEnabled: false,
                    isThirdPartyAgent: false,
                    name: "",
                    description: "",
                    hasSharingRequests: false,
                    connectionDetails,
                    assetGroups: [],
                    operationalReadiness: null,
                    deploymentLocation: Pdms.PrivacyCloudInstanceId.Public,
                    supportedClouds: [Pdms.PrivacyCloudInstanceId.Public],
                    pendingCommandsFound: false,
                    dataResidencyBoundary: null
                };
        }

        return SharedTypes.throwUnsupportedLiteralType(kind);
    }

    public resetDataAgentConnectionDetails(connectionDetails: Pdms.DataAgentConnectionDetails): void {
        //  Reset all common details.
        delete connectionDetails.authenticationType;
        delete connectionDetails.msaSiteId;
        delete connectionDetails.aadAppId;

        //  Per-protocol values.
        switch (connectionDetails.protocol) {
            case Pdms.PrivacyProtocolId.CommandFeedV1:
            case Pdms.PrivacyProtocolId.CommandFeedV2Batch:
            case Pdms.PrivacyProtocolId.CommandFeedV2Continuous:
                connectionDetails.authenticationType = Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth];
                connectionDetails.msaSiteId = 0;
                connectionDetails.aadAppId = "";
                break;

            case Pdms.PrivacyProtocolId.CosmosDeleteSignalV2:
                break;

            default:
                return SharedTypes.throwUnsupportedLiteralType(connectionDetails.protocol);
        }
    }

    public getServicesByName(nameSubstring: string): ng.IPromise<Pdms.STServiceSearchResult[]> {
        return this.pdmsApiService.getServicesByName(nameSubstring).then((response: ng.IHttpPromiseCallbackArg<any>) => {
            return response.data;
        });
    }

    public getServiceById(id: string, kind: Pdms.STEntityKind): ng.IPromise<Pdms.STServiceDetails> {
        return this.pdmsApiService.getServiceById(id, kind).then((response: ng.IHttpPromiseCallbackArg<any>) => {
            return response.data;
        });
    }

    public getOwnersByAuthenticatedUser(): ng.IPromise<Pdms.DataOwner[]> {
        if (this.owners) {
            return this.$promises.resolve(this.owners);
        }

        return this.pdmsApiService.getOwnersByAuthenticatedUser().then((response: ng.IHttpPromiseCallbackArg<any>) => {
            return this.owners = response.data;
        });
    }

    public getDataOwnerWithServiceTree(ownerId: string): ng.IPromise<Pdms.DataOwner> {
        return this.pdmsApiService.getDataOwnerWithServiceTree(ownerId).then((response: ng.IHttpPromiseCallbackArg<Pdms.DataOwner>) => {
            return response.data;
        });
    }

    public updateDataOwner(dataOwner: Pdms.DataOwner): ng.IPromise<Pdms.DataOwner> {
        let operation: ng.IHttpPromise<any>;
        let isLinkedToServiceTree = dataOwner.serviceTree && dataOwner.serviceTree.id;

        if (isLinkedToServiceTree) {
            let serviceTreeOwner: Pdms.STDataOwner = {
                id: dataOwner.id,
                serviceTreeId: dataOwner.serviceTree.id,
                serviceTreeIdKind: dataOwner.serviceTree.kind,
                icmConnectorId: dataOwner.icmConnectorId,
                writeSecurityGroups: dataOwner.writeSecurityGroups,
                tagSecurityGroups: dataOwner.tagSecurityGroups,
                tagApplicationIds: dataOwner.tagApplicationIds,
                sharingRequestContacts: dataOwner.sharingRequestContacts
            };
            operation = this.createOrUpdate(serviceTreeOwner, this.pdmsApiService.createDataOwnerWithServiceTree, this.pdmsApiService.updateDataOwnerWithServiceTree);
        } else {
            operation = this.createOrUpdate(dataOwner, this.pdmsApiService.createDataOwner, this.pdmsApiService.updateDataOwner);
        }

        return operation.then((response: ng.IHttpPromiseCallbackArg<Pdms.DataOwner>) => {
            this.owners = null;
            return response.data;
        });
    }

    public linkDataOwnerToServiceTree(dataOwner: Pdms.DataOwner, serviceTreeEntity: Pdms.STEntityBase): ng.IPromise<Pdms.DataOwner> {
        return this.pdmsApiService.linkDataOwnerToServiceTree(dataOwner.id, serviceTreeEntity)
            .then((response: ng.IHttpPromiseCallbackArg<Pdms.DataOwner>) => {
                this.owners = null;
                return response.data;
            });
    }

    public deleteDataOwner(dataOwner: Pdms.DataOwner): ng.IPromise<any> {
        return this.pdmsApiService.deleteDataOwnerById(dataOwner.id)
            .then((response: ng.IHttpPromiseCallbackArg<Pdms.DataOwner>) => {
                this.owners = null;
                return response.data;
            });
    }

    public getDeleteAgentById(id: string): ng.IPromise<Pdms.DataAgent> {
        return this.pdmsApiService.getDeleteAgentById(id).then((response: ng.IHttpPromiseCallbackArg<any>) => {
            return response.data;
        });
    }

    public updateDataAgent(dataAgent: Pdms.DataAgent): ng.IPromise<Pdms.DataAgent> {
        let operation: ng.IPromise<any>;

        switch (dataAgent.kind) {
            case "delete-agent":
                operation = this.createOrUpdate(dataAgent, this.pdmsApiService.createDeleteAgent, this.pdmsApiService.updateDeleteAgent);
                break;

            default:
                return SharedTypes.throwUnsupportedLiteralType(dataAgent.kind);
        }

        return operation.then((response: ng.IHttpPromiseCallbackArg<Pdms.DataAgent>) => {
            return response.data;
        });
    }

    public deleteDataAgent(dataAgent: Pdms.DataAgent): ng.IPromise<any> {
        return this.pdmsApiService.deleteDataAgentById(dataAgent.id, dataAgent.pendingCommandsFound).catch((e) => {
            if (e.jqXHR.status === 409 && e.jqXHR.responseJSON.error === "hasPendingCommands") {
                dataAgent.pendingCommandsFound = true;
            }
            throw e;
        });
    }

    public deleteAssetGroup(assetGroup: Pdms.AssetGroup): ng.IPromise<any> {
        return this.pdmsApiService.deleteAssetGroupById(assetGroup.id);
    }

    public getAssetTypeMetadata(): ng.IPromise<Pdms.AssetType[]> {
        return this.pdmsApiService.getAssetTypeMetadata().then((response: ng.IHttpPromiseCallbackArg<Pdms.AssetType[]>) => {
            return response.data;
        });
    }

    public getAssetGroupById(id: string): ng.IPromise<Pdms.AssetGroup> {
        return this.pdmsApiService.getAssetGroupById(id).then((response: ng.IHttpPromiseCallbackArg<Pdms.AssetGroup>) => {
            return response.data;
        });
    }

    public getAssetGroupsByOwnerId(ownerId: string): ng.IPromise<Pdms.AssetGroup[]> {
        return this.pdmsApiService.getAssetGroupsByOwnerId(ownerId).then((response: ng.IHttpPromiseCallbackArg<Pdms.AssetGroup[]>) => {
            return response.data;
        });
    }

    public getAssetGroupsCountByOwnerId(ownerId: string): ng.IPromise<number> {
        return this.pdmsApiService.getAssetGroupsCountByOwnerId(ownerId).then((response: ng.IHttpPromiseCallbackArg<number>) => {
            return response.data;
        });
    }

    public getDataAgentsCountByOwnerId(ownerId: string): ng.IPromise<number> {
        return this.pdmsApiService.getDataAgentsCountByOwnerId(ownerId).then((response: ng.IHttpPromiseCallbackArg<number>) => {
            return response.data;
        });
    }

    public getAssetGroupsByDeleteAgentId(deleteAgentId: string): ng.IPromise<Pdms.AssetGroup[]> {
        return this.pdmsApiService.getAssetGroupsByDeleteAgentId(deleteAgentId).then((response: ng.IHttpPromiseCallbackArg<Pdms.AssetGroup[]>) => {
            return response.data;
        });
    }

    public getAssetGroupsByAgentId(agentId: string): ng.IPromise<Pdms.AssetGroup[]> {
        return this.pdmsApiService.getAssetGroupsByAgentId(agentId).then((response: ng.IHttpPromiseCallbackArg<Pdms.AssetGroup[]>) => {
            return response.data;
        });
    }

    public getAssetGroupsForAgent(agent: Pdms.DataAgent, ownerId: string): ng.IPromise<Pdms.DataAgentSupportedAssetGroups> {
        let allAssetGroupsLinkedToDeleteAgent: Pdms.AssetGroup[] = [];
        let allAssetGroupsLinkedToExportAgent: Pdms.AssetGroup[] = [];
        let uniqueUnlinkedAssetGroups: Pdms.AssetGroup[] = [];

        return this.$promises.all([
            this.pdmsApiService.getAssetGroupsByDeleteAgentId(agent.id)
                .then((response: ng.IHttpPromiseCallbackArg<Pdms.AssetGroup[]>) => {
                    allAssetGroupsLinkedToDeleteAgent = response.data;
                }),
            this.pdmsApiService.getAssetGroupsByOwnerId(ownerId)
                .then((response: ng.IHttpPromiseCallbackArg<Pdms.AssetGroup[]>) => {
                    // TODO Bug 15119680 should expose an api from PDMS to get asset groups by export agent ID. Get by owner as a workaround.
                    let protocol = DataAgentHelper.getProtocol(agent);
                    uniqueUnlinkedAssetGroups = DataAgentHelper.getLinkableAssetGroups(protocol, agent.id, response.data);
                    allAssetGroupsLinkedToExportAgent = DataAgentHelper.getAssetGroupsLinkedToExportAgent(agent.id, response.data);
                })
        ]).then(() => {
            let allAssetGroupsLinkedToAny = _.union(
                allAssetGroupsLinkedToDeleteAgent,
                allAssetGroupsLinkedToExportAgent
            );

            return {
                linkedAssetGroups: _.uniq(allAssetGroupsLinkedToAny, (ag: Pdms.AssetGroup) => ag.id),
                unlinkedAssetGroups: uniqueUnlinkedAssetGroups
            };
        });
    }

    public getVariantById(variantId: string): ng.IPromise<Pdms.VariantDefinition> {
        return this.pdmsApiService.getVariantById(variantId).then((response: ng.IHttpPromiseCallbackArg<Pdms.VariantDefinition>) => {
            return response.data;
        });
    }

    public getDataAgentsByOwnerId(ownerId: string): ng.IPromise<Pdms.DataAgent[]> {
        return this.pdmsApiService.getDataAgentsByOwnerId(ownerId).then((response: ng.IHttpPromiseCallbackArg<Pdms.DataAgent[]>) => {
            return response.data;
        });
    }

    public getSharedDataAgents(): ng.IPromise<Pdms.DeleteAgent[]> {
        return this.pdmsApiService.getSharedDataAgents().then((response: ng.IHttpPromiseCallbackArg<Pdms.DeleteAgent[]>) => {
            return response.data;
        });
    }

    public getSharedDataAgentsByOwnerId(ownerId: string): ng.IPromise<Pdms.DeleteAgent[]> {
        return this.pdmsApiService.getSharedDataAgentsByOwnerId(ownerId).then((response: ng.IHttpPromiseCallbackArg<Pdms.DeleteAgent[]>) => {
            return response.data;
        });
    }

    public getDataOwnerByName(ownerName: string): ng.IPromise<Pdms.DataOwner> {
        return this.pdmsApiService.getDataOwnerByName(ownerName).then((response: ng.IHttpPromiseCallbackArg<Pdms.DataOwner>) => {
            return response.data;
        });
    }

    public getDataOwnersBySubstring(ownerSubstring: string): ng.IPromise<Pdms.DataOwner[]> {
        return this.pdmsApiService.getDataOwnersBySubstring(ownerSubstring).then((response: ng.IHttpPromiseCallbackArg<Pdms.DataOwner[]>) => {
            return response.data;
        });
    }

    public getDataAssetsByAssetGroupQualifier(assetGroupQualifier: Pdms.AssetGroupQualifier): ng.IPromise<Pdms.GetDataAssetsByAssetGroupQualifierResponse> {
        return this.pdmsApiService.getDataAssetsByAssetGroupQualifier(assetGroupQualifier).then((response: ng.IHttpPromiseCallbackArg<Pdms.GetDataAssetsByAssetGroupQualifierResponse>) => {
            return response.data;
        });
    }

    public getSharingRequestById(id: string): ng.IPromise<Pdms.SharingRequest> {
        return this.pdmsApiService.getSharingRequestById(id)
            .then((response: ng.IHttpPromiseCallbackArg<Pdms.SharingRequest>) => {
                return response.data;
            });
    }

    public getSharingRequestsByAgentId(agentId: string): ng.IPromise<Pdms.SharingRequest[]> {
        return this.pdmsApiService.getSharingRequestsByAgentId(agentId)
            .then((response: ng.IHttpPromiseCallbackArg<Pdms.SharingRequest[]>) => {
                return response.data;
            });
    }

    public approveSharingRequests(sharingRequestIds: string[]): ng.IPromise<any> {
        return this.pdmsApiService.approveSharingRequests(sharingRequestIds)
            .then((response: ng.IHttpPromiseCallbackArg<void>) => {
                this.owners = null;
                return response.data;
            });
    }

    public denySharingRequests(sharingRequestIds: string[]): ng.IPromise<any> {
        return this.pdmsApiService.denySharingRequests(sharingRequestIds)
            .then((response: ng.IHttpPromiseCallbackArg<void>) => {
                this.owners = null;
                return response.data;
            });
    }

    public getTransferRequestsByTargetOwnerId(ownerId: string): ng.IPromise<Pdms.TransferRequest[]> {
        return this.pdmsApiService.getTransferRequestsByTargetOwnerId(ownerId)
            .then((response: ng.IHttpPromiseCallbackArg<Pdms.TransferRequest[]>) => {
                return response.data;
            });
    }

    public approveTransferRequests(sharingRequestIds: string[]): ng.IPromise<any> {
        return this.pdmsApiService.approveTransferRequests(sharingRequestIds)
            .then((response: ng.IHttpPromiseCallbackArg<void>) => {
                this.owners = null;
                return response.data;
            });
    }

    public denyTransferRequests(sharingRequestIds: string[]): ng.IPromise<any> {
        return this.pdmsApiService.denyTransferRequests(sharingRequestIds)
            .then((response: ng.IHttpPromiseCallbackArg<void>) => {
                this.owners = null;
                return response.data;
            });
    }

    public createTransferRequest(transferRequest: Pdms.TransferRequest): ng.IPromise<void> {
        return this.pdmsApiService.createTransferRequest(transferRequest)
            .then((response: ng.IHttpPromiseCallbackArg<void>) => {
                this.owners = null;
                return response.data;
            });
    }

    public setAgentRelationshipsAsync(setAgentRelationshipRequest: SetAgentRelationshipRequest): ng.IPromise<void> {
        return this.pdmsApiService.setAgentRelationshipsAsync(setAgentRelationshipRequest)
            .then((response: ng.IHttpPromiseCallbackArg<void>) => {
                return response.data;
            });
    }

    public search(terms: string): ng.IPromise<SearchTypes.SearchResults> {
        return this.pdmsApiService.search(terms)
            .then((response: ng.IHttpPromiseCallbackArg<SearchTypes.SearchResults>) => {
                return response.data;
            });
    }

    public createIcmIncident(incident: Pdms.Incident): ng.IPromise<Pdms.Incident> {
        return this.pdmsApiService.createIcmIncident(incident)
            .then((response: ng.IHttpPromiseCallbackArg<Pdms.Incident>) => {
                return response.data;
            });
    }

    public hasAccessForIncidentManager(): ng.IPromise<any> {
        return this.pdmsApiService.hasAccessForIncidentManager();
    }

    /**
     * Creates or updates entity record.
     * @param entity Entity record to create or update.
     * @param create PDMS API method for creating an entity.
     * @param update PDMS API method for updating an entity.
     */
    private createOrUpdate<T extends IEntityWithId>(entity: T, create: PdmsApiEntityMethod<T>, update: PdmsApiEntityMethod<T>): ng.IHttpPromise<any> {
        let isNew = !entity.id;
        return isNew ? create.apply(this.pdmsApiService, [entity]) : update.apply(this.pdmsApiService, [entity]);
    }
}
