import * as angular from "angular";
import { Config, Inject } from "../../module/app.module";

import * as Pdms from "../pdms/pdms-types";
import * as SearchTypes from "../search-types";
import { IMocksService } from "../mocks.service";
import { SetAgentRelationshipRequest } from "../pdms-agent-relationship-types";
import { VariantState } from "../variant/variant-types";
import { ScenarioConfigurator, IScenarioConfigurator } from "../scenario/scenario-configurator";
import { PdmsDataMockHelper } from "./pdms-data-mock-helper";
import { AppConfig } from "../../module/data.module";

/**
 * Mocks PDMS data service.
 */
class PdmsDataMockService implements Pdms.IPdmsDataService {
    @Config()
    @Inject("$provide")
    public static configurePdmsDataMockService($provide: ng.auto.IProvideService): void {
        //  Decorate AJAX service with a function that will add authentication header to each outgoing request.
        $provide.decorator("pdmsDataService", ["$delegate", "$q", "mocksService",
            ($delegate: Pdms.IPdmsDataService,
                      $q: ng.IQService,
                      mocksService: IMocksService,
            ): Pdms.IPdmsDataService => {
                return mocksService.isActive() ? new PdmsDataMockService(
                    $delegate,
                    $q,
                    mocksService,
                ) : $delegate;
            }
        ]);
    }

    private readonly scenarioConfigurator: IScenarioConfigurator<Pdms.IPdmsDataService>;
    private readonly mockHelper: PdmsDataMockHelper;

    constructor(
        private readonly real: Pdms.IPdmsDataService,
        private readonly $promises: ng.IQService,
        private readonly mocksService: IMocksService,
    ) {
        console.debug("Using mocked PDMS service.");

        this.scenarioConfigurator = new ScenarioConfigurator(
            this.real,
            this.mocksService,
        );
        this.mockHelper = new PdmsDataMockHelper(this.$promises);
        this.configureScenarioBasedMocks();
    }

    public getCountriesList(): ng.IPromise<Pdms.Country[]> {
        return this.real.getCountriesList();
    }

    public getPrivacyPolicy(): ng.IPromise<Pdms.PrivacyPolicy> {
        return this.real.getPrivacyPolicy();
    }

    public updateAssetGroup(assetGroup: Pdms.AssetGroup): ng.IPromise<Pdms.AssetGroup> {
        let result = angular.copy(assetGroup);

        if (!result.id) {
            result.id = "964025a4-f66d-4d93-afd4-3b0461e1e72d";
        }

        return this.$promises.resolve(result);
    }

    public createNewDataAgentInstance(kind: Pdms.DataAgentKind): Pdms.DataAgent {
        return this.real.createNewDataAgentInstance(kind);
    }

    public resetDataAgentConnectionDetails(connectionDetails: Pdms.DataAgentConnectionDetails): void {
        this.real.resetDataAgentConnectionDetails(connectionDetails);
    }

    public getServicesByName(nameSubstring: string): ng.IPromise<Pdms.STServiceSearchResult[]> {
        return this.real.getServicesByName(nameSubstring);
    }

    public getServiceById(id: string, kind: Pdms.STEntityKind): ng.IPromise<Pdms.STServiceDetails> {
        return this.real.getServiceById(id, kind);
    }

    public getOwnersByAuthenticatedUser(): ng.IPromise<Pdms.DataOwner[]> {
        return this.scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser")();
    }

    public getDataOwnerWithServiceTree(ownerId: string): ng.IPromise<Pdms.DataOwner> {
        return this.real.getDataOwnerWithServiceTree(ownerId);
    }

    public updateDataOwner(dataOwner: Pdms.DataOwner): ng.IPromise<Pdms.DataOwner> {
        return this.real.updateDataOwner(dataOwner);
    }

    public linkDataOwnerToServiceTree(dataOwner: Pdms.DataOwner, serviceTreeEntity: Pdms.STEntityBase): ng.IPromise<Pdms.DataOwner> {
        return this.real.linkDataOwnerToServiceTree(dataOwner, serviceTreeEntity);
    }

    public deleteDataOwner(dataOwner: Pdms.DataOwner): ng.IPromise<Pdms.DataOwner> {
        return this.real.deleteDataOwner(dataOwner);
    }

    public getDeleteAgentById(id: string): ng.IPromise<Pdms.DataAgent> {
        return this.real.getDeleteAgentById(id);
    }

    public updateDataAgent(dataAgent: Pdms.DataAgent): ng.IPromise<Pdms.DataAgent> {
        return this.real.updateDataAgent(dataAgent);
    }

    public deleteDataAgent(dataAgent: Pdms.DataAgent): ng.IPromise<any> {
        return this.real.deleteDataAgent(dataAgent);
    }

    public getDataAgentsCountByOwnerId(ownerId: string): ng.IPromise<number> {
        return this.real.getDataAgentsCountByOwnerId(ownerId);
    }

    public deleteAssetGroup(assetGroup: Pdms.AssetGroup): ng.IPromise<any> {
        return this.real.deleteAssetGroup(assetGroup);
    }

    public getAssetTypeMetadata(): ng.IPromise<Pdms.AssetType[]> {
        return this.$promises.resolve([{
            id: "CosmosStructuredStream",
            label: "Cosmos Structured Stream",
            props: [{
                id: "PhysicalCluster",
                label: "Physical Cluster",
                description: "The physical cluster name.",
                required: true
            }, {
                id: "VirtualCluster",
                label: "Virtual Cluster",
                description: "The virtual cluster name.",
                required: true
            }, {
                id: "RelativePath",
                label: "Relative Path",
                description: "The relative path to the stream."
            }]
        }, {
            id: "AzureTable",
            label: "Azure Table",
            props: [{
                id: "AccountName",
                label: "Account Name",
                description: "The storage account name.",
                required: true
            }, {
                id: "TableName",
                label: "Table Name",
                description: "The table name.",
            }]
        }, {
            id: "API",
            label: "API",
            props: [{
                id: "Host",
                label: "Host",
                description: "The host name and scheme of the API.",
                required: true
            }, {
                id: "Path",
                label: "Path",
                description: "The Path to the API .",
            }, {
                id: "Method",
                label: "Method",
                description: "The allowed Method for the API .",
            }]
        }]);
    }

    public getDataAssetsByAssetGroupQualifier(assetGroupQualifier: Pdms.AssetGroupQualifier): ng.IPromise<Pdms.GetDataAssetsByAssetGroupQualifierResponse> {
        let dataAssets: Pdms.DataAsset[] = [{
            id: "ID1",
            qualifier: {
                props: {
                    AssetType: "AssetType1",
                    prop1: "PropValue1"
                }
            }
        }, {
            id: "ID2",
            qualifier: {
                props: {
                    AssetType: "AssetType2",
                    prop1: "PropValue2"
                }
            }
        }];

        return this.$promises.resolve({
            dataAssets,
            dataGridSearch: {
                search: "https://datagrid.microsoft.com",
                searchNext: "https://datagrid.microsoft.com/next"
            }
        });
    }

    private appendVariantToAssetGroup(...ags: Pdms.AssetGroup[]): void {
        _.each(ags, (ag: Pdms.AssetGroup) => {
            ag.variants = [{
                variantId: "MockVariant1",
                variantName: "MockVariant1",
                tfsTrackingUris: ["Link1", "Link2"],
                disabledSignalFiltering: false,
                variantState: VariantState.requested
            }, {
                variantId: "MockVariant2",
                variantName: "MockVariant2",
                tfsTrackingUris: ["OnlyLink"],
                disabledSignalFiltering: false,
                variantState: VariantState.requested
            }];
        });
    }

    public getAssetGroupById(id: string): ng.IPromise<Pdms.AssetGroup> {
        if (this.mocksService.isScenarioActive("all-with-variants")) {
            return this.real.getAssetGroupById(id).then((ag: Pdms.AssetGroup) => {
                this.appendVariantToAssetGroup(ag);
                return ag;
            });
        }
        return this.real.getAssetGroupById(id);
    }

    public getAssetGroupsByOwnerId(ownerId: string): ng.IPromise<Pdms.AssetGroup[]> {
        if (this.mocksService.isScenarioActive("all-with-variants")) {
            return this.real.getAssetGroupsByOwnerId(ownerId).then((ags: Pdms.AssetGroup[]) => {
                this.appendVariantToAssetGroup(...ags);
                return ags;
            });
        }

        return this.real.getAssetGroupsByOwnerId(ownerId);
    }

    public getAssetGroupsByDeleteAgentId(deleteAgentId: string): ng.IPromise<Pdms.AssetGroup[]> {
        return this.real.getAssetGroupsByDeleteAgentId(deleteAgentId);
    }

    public getAssetGroupsByAgentId(agentId: string): ng.IPromise<Pdms.AssetGroup[]> {
        return this.real.getAssetGroupsByAgentId(agentId);
    }

    public getAssetGroupsForAgent(agent: Pdms.DataAgent, ownerId: string): ng.IPromise<Pdms.DataAgentSupportedAssetGroups> {
        if (this.mocksService.isScenarioActive("all-with-variants")) {
            return this.real.getAssetGroupsForAgent(agent, ownerId).then((ags: Pdms.DataAgentSupportedAssetGroups) => {
                this.appendVariantToAssetGroup(...ags.linkedAssetGroups, ...ags.unlinkedAssetGroups);
                return ags;
            });
        }
        return this.real.getAssetGroupsForAgent(agent, ownerId);
    }

    public getVariantById(variantId: string): ng.IPromise<Pdms.VariantDefinition> {
        if (this.mocksService.isScenarioActive("all-with-variants")) {
            let variantDefinition: Pdms.VariantDefinition;
            if (variantId === "MockVariant1") {
                variantDefinition = {
                    id: "ID1",
                    name: "Exception xyz",
                    description: "Description of why this variant was issued.",
                    ownerId: "ID1",
                    approver: "jd@msft.com",
                    capabilities: [Pdms.PrivacyCapabilityId.Export, Pdms.PrivacyCapabilityId.Delete],
                    dataTypes: [Pdms.PrivacyDataTypeId.Credentials],
                    subjectTypes: [Pdms.PrivacySubjectTypeId.AADUser, Pdms.PrivacySubjectTypeId.DemographicUser]
                };
            } else {
                variantDefinition = {
                    id: "ID2",
                    name: "Generic exception abc",
                    description: "Description of why this variant was issued.",
                    ownerId: "ID5",
                    approver: "jd@msft.com",
                    capabilities: [Pdms.PrivacyCapabilityId.Export, Pdms.PrivacyCapabilityId.Delete],
                    dataTypes: [Pdms.PrivacyDataTypeId.Credentials],
                    subjectTypes: []
                };
            }

            return this.$promises.resolve(variantDefinition);
        }
        return this.real.getVariantById(variantId);
    }

    public getDataAgentsByOwnerId(ownerId: string): ng.IPromise<Pdms.DataAgent[]> {
        return this.real.getDataAgentsByOwnerId(ownerId);
    }

    public getAssetGroupsCountByOwnerId(ownerId: string): ng.IPromise<number> {
        return this.real.getAssetGroupsCountByOwnerId(ownerId);
    }

    public getDataOwnerByName(ownerName: string): ng.IPromise<Pdms.DataOwner> {
        return this.real.getDataOwnerByName(ownerName);
    }

    public getDataOwnersBySubstring(ownerSubstring: string): ng.IPromise<Pdms.DataOwner[]> {
        return this.real.getDataOwnersBySubstring(ownerSubstring);
    }

    public getSharingRequestById(id: string): ng.IPromise<Pdms.SharingRequest> {
        return this.real.getSharingRequestById(id);
    }

    public getSharingRequestsByAgentId(agentId: string): ng.IPromise<Pdms.SharingRequest[]> {
        return this.real.getSharingRequestsByAgentId(agentId);
    }

    public approveSharingRequests(sharingRequestIds: string[]): ng.IPromise<Pdms.SharingRequest[]> {
        return this.real.approveSharingRequests(sharingRequestIds);
    }

    public denySharingRequests(sharingRequestIds: string[]): ng.IPromise<Pdms.SharingRequest[]> {
        return this.real.denySharingRequests(sharingRequestIds);
    }

    public getTransferRequestsByTargetOwnerId(ownerId: string): ng.IPromise<Pdms.TransferRequest[]> {
        return this.real.getTransferRequestsByTargetOwnerId(ownerId);
    }

    public approveTransferRequests(transferRequestIds: string[]): ng.IPromise<Pdms.TransferRequest[]> {
        return this.real.approveTransferRequests(transferRequestIds);
    }

    public denyTransferRequests(transferRequestIds: string[]): ng.IPromise<Pdms.TransferRequest[]> {
        return this.real.denyTransferRequests(transferRequestIds);
    }

    public createTransferRequest(transferRequest: Pdms.TransferRequest): ng.IPromise<void> {
        return this.real.createTransferRequest(transferRequest);
    }

    public getSharedDataAgents(): ng.IPromise<Pdms.DeleteAgent[]> {
        return this.real.getSharedDataAgents();
    }

    public getSharedDataAgentsByOwnerId(ownerId: string): ng.IPromise<Pdms.DeleteAgent[]> {
        return this.real.getSharedDataAgentsByOwnerId(ownerId);
    }

    public setAgentRelationshipsAsync(setAgentRelationshipRequest: SetAgentRelationshipRequest): ng.IPromise<void> {
        return this.real.setAgentRelationshipsAsync(setAgentRelationshipRequest);
    }

    public search(terms: string): ng.IPromise<SearchTypes.SearchResults> {
        return this.real.search(terms);
    }

    public createIcmIncident(incident: Pdms.Incident): ng.IPromise<Pdms.Incident> {
        return this.real.createIcmIncident(incident);
    }

    public hasAccessForIncidentManager(): ng.IPromise<void> {
        return this.real.hasAccessForIncidentManager();
    }

    private configureScenarioBasedMocks(): void {
        //  Cold start scenarios.
        this.scenarioConfigurator.configureMethodMock("cold-start", "getOwnersByAuthenticatedUser", () => {
            return this.$promises.resolve([]);
        });

        //  Team picker scenarios.
        this.scenarioConfigurator.configureMethodMock("team-picker", "getOwnersByAuthenticatedUser", this.mockHelper.createNFakeTeamsFn(5));
        this.scenarioConfigurator.configureMethodMock("team-picker.one-team", "getOwnersByAuthenticatedUser", this.mockHelper.createNFakeTeamsFn(1));
        this.scenarioConfigurator.configureMethodMock("team-picker.several-teams", "getOwnersByAuthenticatedUser", this.mockHelper.createNFakeTeamsFn(5));
        this.scenarioConfigurator.configureMethodMock("team-picker.gazillion-teams", "getOwnersByAuthenticatedUser", this.mockHelper.createNFakeTeamsFn(100));
    }
}
