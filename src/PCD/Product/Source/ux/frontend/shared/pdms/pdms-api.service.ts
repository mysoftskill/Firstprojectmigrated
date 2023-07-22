import { Service, Inject } from "../../module/app.module";
import { AppConfig } from "../../module/data.module";

import * as Pdms from "../pdms/pdms-types";
import { IMsalTokenManagerFactory } from "../msal-token-manager";
import { IAjaxService, IAjaxServiceFactory, IAjaxServiceOptions } from "../ajax.service";
import { SetAgentRelationshipRequest } from "../pdms-agent-relationship-types";
import { StringUtilities } from "../string-utilities";

//  Provides access to PDMS API. Do not use this directly, use PDMS data service instead.
export interface IPdmsApiService {
    //  Gets the data owners associated with the authenticated user.
    getOwnersByAuthenticatedUser(): ng.IHttpPromise<any>;

    //  Gets the list of nations Microsoft recognizes.
    getCountriesList(): ng.IHttpPromise<any>;

    //  Gets current privacy policy.
    getPrivacyPolicy(): ng.IHttpPromise<any>;

    /**
     * Creates new data asset group record.
     * @param assetGroup Data asset group record to create.
     */
    createAssetGroup(assetGroup: Pdms.AssetGroup): ng.IHttpPromise<any>;

    /**
     * Updates existing data asset group record.
     * @param assetGroup Data asset group record to update.
     */
    updateAssetGroup(assetGroup: Pdms.AssetGroup): ng.IHttpPromise<any>;

    /**
     * Gets the top services that match the given name substring.
     * @param nameSubstring Name to search.
     */
    getServicesByName(nameSubstring: string): ng.IHttpPromise<any[]>;

    /**
     * Gets the detailed service information given the service ID.
     * @param id Service tree entity ID to get.
     * @param kind Service tree entity ID kind.
     */
    getServiceById(id: string, kind: Pdms.STEntityKind): ng.IPromise<any>;

    /**
     * Gets data owner including Service Tree information.
     * @param ownerId ID of the owner.
     */
    getDataOwnerWithServiceTree(ownerId: string): ng.IHttpPromise<any>;

    /**
     * Creates new data owner record.
     * @param dataOwner Data owner record to create.
     */
    createDataOwner(dataOwner: Pdms.DataOwner): ng.IHttpPromise<any>;

    /**
     * Creates new data owner record.
     * @param serviceTreeOwner Service tree related data to create the data owner with.
     */
    createDataOwnerWithServiceTree(serviceTreeOwner: Pdms.STDataOwner): ng.IHttpPromise<any>;

    /**
     * Updates existing data owner record.
     * @param dataOwner Data owner record to update.
     */
    updateDataOwner(dataOwner: Pdms.DataOwner): ng.IHttpPromise<any>;

    /**
     * Updates existing data owner record.
     * @param serviceTreeOwner Service tree related data to update the data owner with.
     */
    updateDataOwnerWithServiceTree(serviceTreeOwner: Pdms.STDataOwner): ng.IHttpPromise<any>;

    /**
     * Links existing data owner record to an existing service tree entity.
     * @param id Data owner ID.
     * @param serviceTreeEntity Service tree entity to link to.
     */
    linkDataOwnerToServiceTree(id: string, serviceTreeEntity: Pdms.STEntityBase): ng.IHttpPromise<any>;

    /**
     * Deletes an existing data owner record.
     * @param id Data owner ID.
     */
    deleteDataOwnerById(id: string): ng.IHttpPromise<any>;

    /**
     * Gets delete agent by ID.
     * @param id Delete agent ID.
     */
    getDeleteAgentById(id: string): ng.IHttpPromise<any>;

    /**
     * Creates new delete data agent record.
     * @param dataAgent Delete data agent record to create.
     */
    createDeleteAgent(dataAgent: Pdms.DeleteAgent): ng.IHttpPromise<any>;

    /**
     * Updates existing delete data agent record.
     * @param dataAgent Delete data agent record to update.
     */
    updateDeleteAgent(dataAgent: Pdms.DeleteAgent): ng.IHttpPromise<any>;

    /**
     * Deletes an existing data agent record.
     * @param id Data agent id.
     */
    deleteDataAgentById(id: string, overridePendingCommands: boolean): ng.IHttpPromise<any>;

    /**
     * Deletes an existing asset group record.
     * @param id Asset group id.
     */
    deleteAssetGroupById(id: string): ng.IHttpPromise<any>;

    //  Gets asset type metadata.
    getAssetTypeMetadata(): ng.IHttpPromise<any>;

    /**
     * Gets asset group for a id.
     * @param id Asset group id.
     */
    getAssetGroupById(id: string): ng.IHttpPromise<any>;

    /**
     * Gets asset groups for a given owner id.
     * @param ownerId Data owner id.
     */
    getAssetGroupsByOwnerId(ownerId: string): ng.IHttpPromise<any>;

    /**
     * Gets asset groups count for a given owner id.
     * @param ownerId Data owner id.
     */
    getAssetGroupsCountByOwnerId(ownerId: string): ng.IHttpPromise<any>;

    /**
     * Gets data agents count for a given owner id.
     * @param ownerId Data owner id.
     */
    getDataAgentsCountByOwnerId(ownerId: string): ng.IHttpPromise<any>;

    /**
     * Gets asset groups for a given delete agent id.
     * @param ownerId Delete agent id.
     */
    getAssetGroupsByDeleteAgentId(deleteAgentId: string): ng.IHttpPromise<any>;

    /**
     * Get asset groups by given agent id (delete or export)
     * @param agentId agent id.
     */
    getAssetGroupsByAgentId(agentId: string): ng.IHttpPromise<any>;

    /**
     * Gets the variant definition for a given variant id.
     * @param variantId Variant id.
     */
    getVariantById(variantId: string): ng.IHttpPromise<any>;

    /**
     * Gets data agents for a given owner id.
     * @param ownerId Data owner id.
     */
    getDataAgentsByOwnerId(ownerId: string): ng.IHttpPromise<any>;

    /**
     * Get shared data agents by owner id
     */
    getSharedDataAgentsByOwnerId(ownerId: string): ng.IHttpPromise<any>;

    /**
     * gets all shared delete agents
     */
    getSharedDataAgents(): ng.IHttpPromise<any>;

    /**
     * set agent relationship with data assets
     */
    setAgentRelationshipsAsync(agentRelationshipRequest: SetAgentRelationshipRequest): ng.IHttpPromise<any>;

    /**
     * Gets data owner by exact string match.
     * @param ownerName Data owner name to find.
     */
    getDataOwnerByName(ownerName: string): ng.IHttpPromise<any>;

    /**
     * Gets data owner names by substring match.
     * @param ownerSubstring Substring of data owner names to find.
     */
    getDataOwnersBySubstring(ownerSubstring: string): ng.IHttpPromise<any>;

    /**
     * Gets data assets by AssetGroup qualifier.
     * @param assetGroupQualifier AssetGroup qualifier.
     */
    getDataAssetsByAssetGroupQualifier(assetGroupQualifier: Pdms.AssetGroupQualifier): ng.IHttpPromise<any>;

    /**
     * Gets a sharing request.
     * @param id to locate a sharing request.
     */
    getSharingRequestById(id: string): ng.IHttpPromise<any>;

    /**
     * Gets all sharing requests for an agent.
     * @param agentId to locate all sharing requests associated with an agent.
     */
    getSharingRequestsByAgentId(agentId: string): ng.IHttpPromise<any>;

    /**
     * Approves a list of Sharing Requests
     * @param sharingRequestIds to locate the sharing requests
     */
    approveSharingRequests(sharingRequestIds: string[]): ng.IHttpPromise<any>;

    /**
     * Denies a list of Sharing Requests
     * @param sharingRequestIds to locate the sharing requests
     */
    denySharingRequests(sharingRequestIds: string[]): ng.IHttpPromise<any>;

    /**
     * Gets all transfer requests for an target owner.
     * @param ownerId to locate all transfer requests associated with a target owner.
     */
    getTransferRequestsByTargetOwnerId(ownerId: string): ng.IHttpPromise<any>;

    /**
     * Accepts a list of transfer requests
     * @param transferRequestIds to locate the transfer requests.
     */
    approveTransferRequests(transferRequestIds: string[]): ng.IHttpPromise<any>;

    /**
     * Deletes a list of transfer requests
     * @param transferRequestIds to locate the transfer requests.
     */
    denyTransferRequests(transferRequestIds: string[]): ng.IHttpPromise<any>;

    /**
    * Creates a transfer request to transfer a list of asset groups
    * from the source owner to a target owner.
    * @param transferRequest to be created.
    */
    createTransferRequest(transferRequest: Pdms.TransferRequest): ng.IHttpPromise<any>;

    /**
     * Searches PDMS for terms.
     * @param terms Terms to search for.
     */
    search(terms: string): ng.IHttpPromise<any>;

    /**
    * Creates an IcM Incident
    * @param incident the incident to create on IcM
    */
    createIcmIncident(incident: Pdms.Incident): ng.IHttpPromise<any>;

    /** 
     * Checks if the user has access to issue incident manager operations. 
     **/
    hasAccessForIncidentManager(): ng.IHttpPromise<any>;
}

@Service({
    name: "pdmsApiService"
})
@Inject("$q", "msalTokenManagerFactory", "appConfig", "ajaxServiceFactory")
class PdmsApiService implements IPdmsApiService {
    public static $inject = ["$q", "appConfig", "ajaxServiceFactory"];

    private ajaxService: IAjaxService;

    constructor(
        private readonly $promise: ng.IQService,
        private readonly msalTokenManagerFactory: IMsalTokenManagerFactory,
        private readonly appConfig: AppConfig,
        private readonly ajaxServiceFactory: IAjaxServiceFactory
    ) {
        let ajaxOptions: IAjaxServiceOptions = {
            authTokenManager: msalTokenManagerFactory.createInstance(this.appConfig.azureAdAppId)
        };
        this.ajaxService = this.ajaxServiceFactory.createInstance(ajaxOptions);
    }

    public getCountriesList(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getcountrieslist",
            serviceName: "PdmsUx",
            operationName: "GetCountriesList",
            cache: true
        });
    }

    public getPrivacyPolicy(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getprivacypolicy",
            serviceName: "PdmsUx",
            operationName: "GetPrivacyPolicy"
        });
    }

    public createAssetGroup(assetGroup: Pdms.AssetGroup): ng.IHttpPromise<any> {
        return this.ajaxService.put({
            url: "/api/createassetgroup",
            serviceName: "PdmsUx",
            operationName: "CreateDataAssetGroup",
            data: assetGroup
        });
    }

    public updateAssetGroup(assetGroup: Pdms.AssetGroup): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/api/updateassetGroup",
            serviceName: "PdmsUx",
            operationName: "UpdateAssetGroup",
            dataType: "json",
            data: assetGroup
        });
    }

    public getOwnersByAuthenticatedUser(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getownersbyauthenticateduser",
            serviceName: "PdmsUx",
            operationName: "GetOwnersByAuthenticatedUser"
        });
    }

    public getServicesByName(nameSubstring: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getservicesbyname",
            serviceName: "PdmsUx",
            operationName: "GetServicesByName",
            data: { nameSubstring },
        });
    }

    public getServiceById(id: string, kind: Pdms.STEntityKind): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getservicebyid",
            serviceName: "PdmsUx",
            operationName: "GetServiceById",
            data: { id, kind },
        });
    }

    public getDataOwnerWithServiceTree(id: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getdataownerwithservicetree",
            serviceName: "PdmsUx",
            operationName: "GetDataOwnerWithServiceTree",
            data: { id },
        });
    }

    public createDataOwner(dataOwner: Pdms.DataOwner): ng.IHttpPromise<any> {
        return this.ajaxService.put({
            url: "/api/createdataowner",
            serviceName: "PdmsUx",
            operationName: "CreateDataOwner",
            data: dataOwner,
        });
    }

    public createDataOwnerWithServiceTree(serviceTreeOwner: Pdms.STDataOwner): ng.IHttpPromise<any> {
        return this.ajaxService.put({
            url: "/api/createdataownerwithservicetree",
            serviceName: "PdmsUx",
            operationName: "CreateDataOwnerWithServiceTree",
            data: serviceTreeOwner
        });
    }

    public updateDataOwner(dataOwner: Pdms.DataOwner): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/api/updatedataowner",
            serviceName: "PdmsUx",
            operationName: "UpdateDataOwner",
            data: dataOwner
        });
    }

    public updateDataOwnerWithServiceTree(serviceTreeOwner: Pdms.STDataOwner): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/api/updatedataownerwithservicetree",
            serviceName: "PdmsUx",
            operationName: "UpdateDataOwnerWithServiceTree",
            data: serviceTreeOwner
        });
    }

    public linkDataOwnerToServiceTree(id: string, serviceTreeEntity: Pdms.STEntityBase): ng.IHttpPromise<any> {
        let queryString = StringUtilities.queryStringOf({
            id,
            serviceTreeId: serviceTreeEntity.id,
            serviceTreeIdKind: serviceTreeEntity.kind
        });

        return this.ajaxService.post({
            url: `/api/linkdataownertoservicetree?${queryString}`,
            serviceName: "PdmsUx",
            operationName: "LinkDataOwnerToServiceTree"
        });
    }

    public deleteDataOwnerById(id: string): ng.IHttpPromise<any> {
        return this.ajaxService.del({
            url: "/api/deletedataownerbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteDataOwnerById",
            data: { id }
        });
    }

    public getDeleteAgentById(id: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getdeleteagentbyid",
            serviceName: "PdmsUx",
            operationName: "GetDeleteAgentById",
            data: { id }
        });
    }

    public createDeleteAgent(dataAgent: Pdms.DeleteAgent): ng.IHttpPromise<any> {
        dataAgent["test"] = "test";
        return this.ajaxService.put({
            url: "/api/createdeleteagent",
            serviceName: "PdmsUx",
            operationName: "CreateDeleteAgent",
            data: dataAgent,
        });
    }

    public updateDeleteAgent(dataAgent: Pdms.DeleteAgent): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/api/updatedeleteagent",
            serviceName: "PdmsUx",
            operationName: "UpdateDeleteAgent",
            dataType: "json",
            data: dataAgent
        });
    }

    public deleteDataAgentById(id: string, overridePendingCommands: boolean): ng.IHttpPromise<any> {
        return this.ajaxService.del({
            url: "/api/deletedataagentbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteDataAgentById",
            data: { id, overridePendingCommands }
        });
    }

    public deleteAssetGroupById(id: string): ng.IHttpPromise<any> {
        return this.ajaxService.del({
            url: "/api/deleteassetgroupbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteAssetGroupById",
            data: { id }
        });
    }

    public getAssetTypeMetadata(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getassettypemetadata",
            serviceName: "PdmsUx",
            operationName: "GetAssetTypeMetadata"
        });
    }

    public getAssetGroupById(id: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getassetgroupbyid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupById",
            data: { id }
        });
    }

    public getAssetGroupsByOwnerId(ownerId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getassetgroupsbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupsByOwnerId",
            data: { ownerId }
        });
    }

    public getAssetGroupsCountByOwnerId(ownerId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getassetgroupscountbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupsCountByOwnerId",
            data: { ownerId }
        });
    }

    public getDataAgentsCountByOwnerId(ownerId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getdataagentscountbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetDataAgentsCountByOwnerId",
            data: { ownerId }
        });
    }

    public getAssetGroupsByDeleteAgentId(deleteAgentId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getassetgroupsbydeleteagentid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupsByDeleteAgentId",
            data: { deleteAgentId }
        });
    }

    public getAssetGroupsByAgentId(agentId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getassetgroupsbyagentid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupsByAgentId",
            data: { agentId }
        });
    }


    public getVariantById(variantId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getvariantbyid",
            serviceName: "PdmsUx",
            operationName: "GetVariantById",
            data: { variantId }
        });
    }

    public getDataAgentsByOwnerId(ownerId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getdataagentsbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetDataAgentsByOwnerId",
            data: { ownerId }
        });
    }

    public getSharedDataAgents(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getshareddataagents",
            serviceName: "PdmsUx",
            operationName: "GetSharedDataAgents",
            data: {}
        });
    }

    public getSharedDataAgentsByOwnerId(ownerId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getshareddataagentsbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetSharedDataAgentsByOwnerId",
            data: { ownerId }
        });
    }

    public getDataOwnerByName(ownerName: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getdataownerbyname",
            serviceName: "PdmsUx",
            operationName: "GetDataOwnerByName",
            data: { ownerName }
        });
    }

    public getDataOwnersBySubstring(ownerSubstring: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getdataownersbysubstring",
            serviceName: "PdmsUx",
            operationName: "GetDataOwnersBySubstring",
            data: { ownerSubstring }
        });
    }

    public getDataAssetsByAssetGroupQualifier(assetGroupQualifier: Pdms.AssetGroupQualifier): ng.IHttpPromise<any> {
        let assetGroupQualifierJson = JSON.stringify(assetGroupQualifier);

        return this.ajaxService.get({
            url: "/api/getdataassetsbyassetgroupqualifier",
            serviceName: "PdmsUx",
            operationName: "GetDataAssetsByAssetGroupQualifier",
            data: { assetGroupQualifierJson }
        });
    }

    public getSharingRequestById(id: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getsharingrequestbyid",
            serviceName: "PdmsUx",
            operationName: "GetSharingRequestById",
            data: { id }
        });
    }

    public getSharingRequestsByAgentId(agentId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getsharingrequestsbyagentid",
            serviceName: "PdmsUx",
            operationName: "GetSharingRequestsByAgentId",
            data: { agentId }
        });
    }

    public approveSharingRequests(sharingRequestIds: string[]): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/api/approvesharingrequests",
            serviceName: "PdmsUx",
            operationName: "ApproveSharingRequests",
            data: sharingRequestIds
        });
    }

    public denySharingRequests(sharingRequestIds: string[]): ng.IHttpPromise<any> {
        return this.ajaxService.del({
            url: "/api/denysharingrequests",
            serviceName: "PdmsUx",
            operationName: "DenySharingRequests",
            data: { sharingRequestIds }
        });
    }

    public getTransferRequestsByTargetOwnerId(ownerId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/asset-transfer-request/api/gettransferrequestsbytargetownerid",
            serviceName: "PdmsUx",
            operationName: "GetTransferRequestsByTargetOwnerId",
            data: { ownerId }
        });
    }

    public approveTransferRequests(transferRequestIds: string[]): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/asset-transfer-request/api/approvetransferrequests",
            serviceName: "PdmsUx",
            operationName: "ApproveTransferRequests",
            data: transferRequestIds
        });
    }

    public denyTransferRequests(transferRequestIds: string[]): ng.IHttpPromise<any> {
        return this.ajaxService.del({
            url: "/asset-transfer-request/api/denytransferrequests",
            serviceName: "PdmsUx",
            operationName: "DenyTransferRequests",
            data: { transferRequestIds }
        });
    }

    public createTransferRequest(transferRequest: Pdms.TransferRequest): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/asset-transfer-request/api/createtransferrequest",
            serviceName: "PdmsUx",
            operationName: "CreateTransferRequest",
            data: transferRequest
        });
    }

    public setAgentRelationshipsAsync(setAgentRelationshipRequest: SetAgentRelationshipRequest): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/api/setagentrelationships",
            serviceName: "PdmsUx",
            operationName: "SetAgentRelationships",
            data: setAgentRelationshipRequest
        });
    }

    public search(terms: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/search",
            serviceName: "PdmsUx",
            operationName: "Search",
            data: { terms },
            cache: true
        });
    }

    public createIcmIncident(incident: Pdms.Incident): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/api/createicmincident",
            serviceName: "PdmsUx",
            operationName: "CreateIcmIncident",
            data: incident
        });
    }

    public hasAccessForIncidentManager(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/hasaccessforincidentmanager",
            serviceName: "PdmsUx",
            operationName: "HasAccessForIncidentManager"
        });
    }
}
