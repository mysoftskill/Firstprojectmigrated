import * as angular from "angular";
import { TestSpec, SpyCache } from "../../shared-tests/spec.base";

import * as Pdms from "../pdms/pdms-types";
import * as SearchTypes from "../search-types";
import { IPdmsApiService } from "./pdms-api.service";
import { SetAgentRelationshipRequest } from "../pdms-agent-relationship-types";

describe("PDMS data service", () => {
    let spec: TestSpec;
    let pdmsDataService: Pdms.IPdmsDataService;
    let pdmsApiServiceMock: SpyCache<IPdmsApiService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_pdmsDataService_: Pdms.IPdmsDataService, _pdmsApiService_: IPdmsApiService) => {
            pdmsDataService = _pdmsDataService_;
            pdmsApiServiceMock = new SpyCache(_pdmsApiService_);
        });
    });

    it("gets privacy policy first from API then from cache", () => {
        let privacyPolicy: Pdms.PrivacyPolicy = {
            dataTypes: {
                "18843042-4ca1-4879-84e2-062c1c28f641": {
                    id: "18843042-4ca1-4879-84e2-062c1c28f641",
                    name: "type",
                    description: "type description",
                    capabilities: ["95cca320-ced4-4c4a-b7be-21b22d1fc25c"]
                }
            },
            capabilities: {
                "95cca320-ced4-4c4a-b7be-21b22d1fc25c": {
                    id: "95cca320-ced4-4c4a-b7be-21b22d1fc25c",
                    name: "capability name",
                    description: "capability description",
                    protocols: ["50a257bf-0c15-4e44-9000-f381b447037b"]
                }
            },
            protocols: {
                "50a257bf-0c15-4e44-9000-f381b447037b": {
                    id: "50a257bf-0c15-4e44-9000-f381b447037b",
                    name: "protocol name",
                    description: "protocol description"
                }
            },
            supportedClouds: {
                "Public": {
                    id: "50a257bf-0c15-4e44-9000-f381b447037b",
                    name: "public",
                    description: "public name",
                    supportedClouds: ["50a257bf-0c15-4e44-9000-f381b447037b"]
                }
            }
        };
        pdmsApiServiceMock.getFor("getPrivacyPolicy").and.returnValue(spec.asHttpPromise(privacyPolicy));

        let retrievedFromApiCall: Pdms.PrivacyPolicy;
        pdmsDataService.getPrivacyPolicy().then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(privacyPolicy);

        //  Another call should use cache.
        pdmsApiServiceMock.failIfCalled("getPrivacyPolicy");

        let retrievedFromCache: Pdms.PrivacyPolicy;
        pdmsDataService.getPrivacyPolicy().then(data => { retrievedFromCache = data; });
        spec.runDigestCycle();
        expect(retrievedFromCache).toEqual(privacyPolicy);
    });

    it("gets data assets from API based on asset group qualifier", () => {
        // arrange
        let expectedDataAssets: Pdms.GetDataAssetsByAssetGroupQualifierResponse = {
            dataAssets: [{
                id: "DataAsset1ID",
                qualifier: {
                    props: {
                        AssetType: "AssetType1"
                    }
                }
            }],
            dataGridSearch: {
                search: "https://datagrid.microsoft.com",
                searchNext: "https://datagrid.microsoft.com/next"
            }
        };
        let returnedDataAssets = expectedDataAssets;
        let assetGroupQualifier: Pdms.AssetGroupQualifier = {
            props: {
                AssetType: "AssetType1"
            }
        };
        pdmsApiServiceMock.getFor("getDataAssetsByAssetGroupQualifier").and.returnValue(spec.asHttpPromise(returnedDataAssets));
        let retrievedFromApiCall: Pdms.GetDataAssetsByAssetGroupQualifierResponse;

        // act
        pdmsDataService.getDataAssetsByAssetGroupQualifier(assetGroupQualifier)
            .then(data => {
                retrievedFromApiCall = data;
            });
        spec.runDigestCycle();

        // assert
        expect(retrievedFromApiCall).toEqual(expectedDataAssets);
    });

    describe("updateDataOwner", () => {
        it("creates owner if no service tree is specified", (done: DoneFn) => {
            let dataOwner: Pdms.DataOwner = {
                id: null,
                name: "any",
                description: "any",
                alertContacts: ["anyAC"],
                announcementContacts: ["anyAC"],
                sharingRequestContacts: [],
                assetGroups: null,
                dataAgents: null,
                icmConnectorId: "08DC47AF-F9B8-491E-85B4-85D29F82B7A9",
                writeSecurityGroups: ["anySG"],
                tagSecurityGroups: ["anySG"],
                tagApplicationIds: ["8P09E048-7AA3-49B2-AD96-1F7C5A3B75C2"],
                serviceTree: null
            };

            pdmsApiServiceMock.getFor("createDataOwner").and.returnValue(spec.asHttpPromise(null));
            pdmsApiServiceMock.failIfCalled("createDataOwnerWithServiceTree");

            pdmsDataService.updateDataOwner(dataOwner).then(() => {
                expect(pdmsApiServiceMock.getFor("createDataOwner")).toHaveBeenCalledWith(dataOwner);
                done();
            });
            spec.runDigestCycle();
        });

        it("creates using service tree if a service tree is specified", (done: DoneFn) => {
            let dataOwner: Pdms.DataOwner = {
                id: null,
                name: null,
                description: null,
                alertContacts: null,
                announcementContacts: null,
                sharingRequestContacts: null,
                assetGroups: null,
                dataAgents: null,
                icmConnectorId: "6A09E048-7AA3-49B2-AD96-1F7C5A3B75C2",
                writeSecurityGroups: ["anySG"],
                tagSecurityGroups: ["anySG"],
                tagApplicationIds: ["8P09E048-7AA3-49B2-AD96-1F7C5A3B75C2"],
                serviceTree: {
                    id: "anyServiceId",
                    kind: "service"
                }
            };

            pdmsApiServiceMock.getFor("createDataOwnerWithServiceTree").and.returnValue(spec.asHttpPromise(null));
            pdmsApiServiceMock.failIfCalled("createDataOwner");

            pdmsDataService.updateDataOwner(dataOwner).then(() => {
                expect(pdmsApiServiceMock.getFor("createDataOwnerWithServiceTree")).toHaveBeenCalledWith({
                    id: dataOwner.id,
                    serviceTreeId: dataOwner.serviceTree.id,
                    serviceTreeIdKind: dataOwner.serviceTree.kind,
                    icmConnectorId: "6A09E048-7AA3-49B2-AD96-1F7C5A3B75C2",
                    writeSecurityGroups: dataOwner.writeSecurityGroups,
                    tagSecurityGroups: dataOwner.tagSecurityGroups,
                    tagApplicationIds: dataOwner.tagApplicationIds,
                    sharingRequestContacts: dataOwner.sharingRequestContacts
                });
                done();
            });
            spec.runDigestCycle();
        });

        it("updates owner if an id is specified", (done: DoneFn) => {
            let dataOwner: Pdms.DataOwner = {
                id: "anyId",
                name: null,
                description: null,
                alertContacts: null,
                announcementContacts: null,
                sharingRequestContacts: [],
                assetGroups: null,
                dataAgents: null,
                icmConnectorId: "2CCDAEA2-D0AB-4954-BDC1-CC0A9DEAD4E7",
                writeSecurityGroups: ["anySG"],
                tagSecurityGroups: ["anySG"],
                tagApplicationIds: ["8P09E048-7AA3-49B2-AD96-1F7C5A3B75C2"],
                serviceTree: {
                    id: null,
                    kind: "service"
                }
            };

            pdmsApiServiceMock.getFor("updateDataOwner").and.returnValue(spec.asHttpPromise(null));

            pdmsDataService.updateDataOwner(dataOwner).then(() => {
                expect(pdmsApiServiceMock.getFor("updateDataOwner")).toHaveBeenCalledWith(dataOwner);
                done();
            });
            spec.runDigestCycle();
        });

        it("updates owner with service tree if an id is specified", (done: DoneFn) => {
            let dataOwner: Pdms.DataOwner = {
                id: "anyId",
                name: null,
                description: null,
                alertContacts: null,
                announcementContacts: null,
                sharingRequestContacts: [],
                assetGroups: null,
                dataAgents: null,
                icmConnectorId: "BC6AC439-408F-4851-B2A2-20B5B6699621",
                writeSecurityGroups: ["anySG"],
                tagSecurityGroups: ["anySG"],
                tagApplicationIds: ["8P09E048-7AA3-49B2-AD96-1F7C5A3B75C2"],
                serviceTree: {
                    id: "anyServiceId",
                    kind: "teamGroup"
                }
            };

            pdmsApiServiceMock.getFor("updateDataOwnerWithServiceTree").and.returnValue(spec.asHttpPromise(null));

            pdmsDataService.updateDataOwner(dataOwner).then(() => {
                expect(pdmsApiServiceMock.getFor("updateDataOwnerWithServiceTree")).toHaveBeenCalledWith({
                    id: dataOwner.id,
                    serviceTreeId: dataOwner.serviceTree.id,
                    serviceTreeIdKind: dataOwner.serviceTree.kind,
                    icmConnectorId: "BC6AC439-408F-4851-B2A2-20B5B6699621",
                    writeSecurityGroups: dataOwner.writeSecurityGroups,
                    tagSecurityGroups: dataOwner.tagSecurityGroups,
                    tagApplicationIds: dataOwner.tagApplicationIds,
                    sharingRequestContacts: dataOwner.sharingRequestContacts
                });
                done();
            });
            spec.runDigestCycle();
        });
    });

    describe("linkDataOwnerToServiceTree", () => {
        it("links existing data owner with a service tree entity", (done: DoneFn) => {
            let dataOwner: Pdms.DataOwner = {
                id: "aa53a60e-d037-41ee-8a39-1ff721e0297b",
                name: null,
                description: null,
                alertContacts: null,
                announcementContacts: null,
                sharingRequestContacts: [],
                assetGroups: null,
                dataAgents: null,
                writeSecurityGroups: ["anySG"],
                tagSecurityGroups: ["anySG"],
                tagApplicationIds: ["8P09E048-7AA3-49B2-AD96-1F7C5A3B75C2"],
                serviceTree: null
            };
            let serviceTreeEntity: Pdms.STEntityBase = {
                id: "72a11d61-54d6-4e0b-bd1a-0e9812bfa302",
                kind: "serviceGroup"
            };

            pdmsApiServiceMock.getFor("linkDataOwnerToServiceTree").and.returnValue(spec.asHttpPromise(null));

            pdmsDataService.linkDataOwnerToServiceTree(dataOwner, serviceTreeEntity).then(() => {
                expect(pdmsApiServiceMock.getFor("linkDataOwnerToServiceTree")).toHaveBeenCalledWith(dataOwner.id, serviceTreeEntity);
                done();
            });
            spec.runDigestCycle();
        });
    });

    describe("updateDeleteAgent()", () => {
        it("creates a new delete data agent", () => {
            let dataAgent: Pdms.DeleteAgent = {
                id: "",                                         //  Empty ID signifies new entity.
                kind: "delete-agent",
                ownerId: "bb85efd2-b547-49cb-b3e9-6471784e3ec4",
                sharingEnabled: false,
                isThirdPartyAgent: false,
                name: "data agent name",
                description: "data agent description",
                hasSharingRequests: false,
                connectionDetails: {
                    "PreProd": {
                        releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                        protocol: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2,
                        authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
                        msaSiteId: 0
                    }
                },
                assetGroups: [],
                operationalReadiness: {
                    isLoggingEnabled: true,
                    isLoggingCompliant: true,
                    isLoggingIncludesCommandId: true,
                    isReliabilityAlertsTrigger: true,
                    isLatencyAlertsTrigger: true,
                    isMonitoringSla: true,
                    isScalableForCommandRate: true,
                    isAlertsInIcm: true,
                    isDriDocumentation: true,
                    isDriEscalation: true,
                    isIncidentSeverityDoc: true,
                    isGuidesPublished: true,
                    isE2eValidation: true,
                    isCertExpiryAlerts: true,
                    isCertChangeDoc: true,
                    isServiceRecoveryPlan: true,
                    isServiceInProd: true,
                    isDisasterRecoveryPlan: true,
                    isDisasterRecoveryTested: true,
                },
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            };
            pdmsApiServiceMock.getFor("createDeleteAgent").and.returnValue(spec.asHttpPromise(dataAgent));

            let retrievedFromApiCall: Pdms.DataAgent;
            pdmsDataService.updateDataAgent(dataAgent).then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(dataAgent);
        });

        it("updates existing delete data agent", () => {
            let dataAgent: Pdms.DeleteAgent = {
                id: "acb88bcc-961a-4a9d-80e8-216a05cafe9d",
                kind: "delete-agent",
                ownerId: "3c4b5d16-b1f7-4324-a1d9-d2a77a72fc08",
                sharingEnabled: false,
                isThirdPartyAgent: false,
                name: "updated data agent name",
                description: "updated data agent description",
                hasSharingRequests: false,
                connectionDetails: {
                    "PreProd": {
                        releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                        protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                        authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
                        msaSiteId: 1
                    }
                },
                assetGroups: [],
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            };
            pdmsApiServiceMock.getFor("updateDeleteAgent").and.returnValue(spec.asHttpPromise(dataAgent));

            let retrievedFromApiCall: Pdms.DataAgent;
            pdmsDataService.updateDataAgent(dataAgent).then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(dataAgent);
        });

        it("throws if unsupported data agent kind was supplied", () => {
            //  This (legal) kind is not supported yet. Remove this assert and add a happy path test case.
            let dataAgent1: Pdms.LegacyDataAgent = {
                id: "",                                         //  Empty ID signifies new entity.
                kind: "legacy-agent",
                ownerId: "8ceca44a-30e7-4eb6-a5af-e46a4659f6a2",
                sharingEnabled: false,
                isThirdPartyAgent: false,
                name: "data agent name",
                description: "data agent description",
                hasSharingRequests: false,
                connectionDetails: {
                    "PreProd": <Pdms.DataAgentConnectionDetails> {
                        releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                        protocol: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2
                    }
                },
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            };
            expect(() => pdmsDataService.updateDataAgent(dataAgent1)).toThrowError();

            //  This (legal) kind is not supported yet. Remove this assert and add a happy path test case.
            let dataAgent2: Pdms.PdosDataAgent = {
                id: "",                                         //  Empty ID signifies new entity.
                kind: "pdos-agent",
                ownerId: "8ceca44a-30e7-4eb6-a5af-e46a4659f6a2",
                sharingEnabled: false,
                isThirdPartyAgent: false,
                name: "data agent name",
                description: "data agent description",
                hasSharingRequests: false,
                connectionDetails: {
                    "PreProd": <Pdms.DataAgentConnectionDetails> {
                        releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                        protocol: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2
                    }
                },
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            };
            expect(() => pdmsDataService.updateDataAgent(dataAgent2)).toThrowError();

            //  Completely unsupported kind.
            let dataAgent3: Pdms.PdosDataAgent = {
                id: "",                                         //  Empty ID signifies new entity.
                kind: <any> "unsupported-agent",
                ownerId: "8ceca44a-30e7-4eb6-a5af-e46a4659f6a2",
                sharingEnabled: false,
                isThirdPartyAgent: false,
                name: "data agent name",
                description: "data agent description",
                hasSharingRequests: false,
                connectionDetails: {
                    "PreProd": <Pdms.DataAgentConnectionDetails> {
                        releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                        protocol: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2
                    }
                },
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            };
            expect(() => pdmsDataService.updateDataAgent(dataAgent3)).toThrowError();
        });
    });

    it("gets existing data agent", () => {
        let dataAgent: Pdms.DataAgent = {
            kind: "delete-agent",
            id: "58596577-4efb-4694-b993-3f533681eac1",
            ownerId: "07b5b571-81e3-4aca-bde8-7ddec0c98adc",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            name: "data agent name",
            description: "data agent description",
            hasSharingRequests: false,
            connectionDetails: {
                "PreProd": <Pdms.DataAgentConnectionDetails> {
                    protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth]
                }
            },
            assetGroups: [],
            operationalReadiness: null,
            deploymentLocation: null,
            supportedClouds: null,
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
        };
        pdmsApiServiceMock.getFor("getDeleteAgentById").and.returnValue(spec.asHttpPromise(dataAgent));

        let retrievedFromApiCall: Pdms.DataAgent;
        pdmsDataService.getDeleteAgentById("7c5d0095-fa2e-4aee-976b-833db128d475").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(dataAgent);
    });

    it("gets asset type metadata", () => {
        let metadata: Pdms.AssetType[] = [{
            id: "AssetType1",
            label: "Asset Type 1",
            props: [{
                id: "Prop1",
                label: "Prop 1",
                description: "Description prop 1"
            }, {
                id: "Prop2",
                label: "Prop 2",
                description: "Description prop 2",
                required: true
            }]
        }, {
            id: "AssetType2",
            label: "Asset Type 2",
            props: [{
                id: "Prop3",
                label: "Prop 3",
                description: "Description prop 3",
                required: true
            }, {
                id: "Prop4",
                label: "Prop 4",
                description: "Description prop 4"
            }]
        }];
        pdmsApiServiceMock.getFor("getAssetTypeMetadata").and.returnValue(spec.asHttpPromise(metadata));

        let retrievedFromApiCall: Pdms.AssetType[];
        pdmsDataService.getAssetTypeMetadata().then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(metadata);
    });

    describe("getOwnersByAuthenticatedUser()", () => {
        const owner1: Pdms.DataOwner = {
            id: "07b5b571-81e3-4aca-bde8-7ddec0c98adc",
            name: "owner1",
            description: "Described",
            alertContacts: [],
            announcementContacts: [],
            sharingRequestContacts: [],
            assetGroups: [],
            dataAgents: [],
            writeSecurityGroups: [],
            serviceTree: null
        };
        const owner2: Pdms.DataOwner = {
            id: "5a4a5db4-f708-4666-9b15-fbaac8d57267",
            name: "owner2",
            description: "Described",
            alertContacts: [],
            announcementContacts: [],
            sharingRequestContacts: [],
            assetGroups: [],
            dataAgents: [],
            writeSecurityGroups: [],
            serviceTree: null
        };
        const owners = [owner1, owner2];

        beforeEach(() => {
            pdmsApiServiceMock.getFor("getOwnersByAuthenticatedUser").and.returnValue(spec.asHttpPromise(owners));
        });

        it("gets existing data owners for an authenticated user", () => {
            let retrievedFromApiCall: Pdms.DataOwner[];
            pdmsDataService.getOwnersByAuthenticatedUser().then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(owners);
        });

        it("caches API call results", () => {
            let retrievedFromApiCall: Pdms.DataOwner[];
            pdmsDataService.getOwnersByAuthenticatedUser().then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(owners);

            //  Should not call this API.
            pdmsApiServiceMock.failIfCalled("getOwnersByAuthenticatedUser");

            retrievedFromApiCall = null;
            pdmsDataService.getOwnersByAuthenticatedUser().then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(owners);
        });

        it("resets cache if data owner was updated", () => {
            let retrievedFromApiCall: Pdms.DataOwner[];
            pdmsDataService.getOwnersByAuthenticatedUser().then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(owners);

            //  Update owner.
            pdmsApiServiceMock.getFor("updateDataOwner").and.returnValue(spec.asHttpPromise(null));
            pdmsDataService.updateDataOwner(owner2);
            spec.runDigestCycle();

            retrievedFromApiCall = null;
            pdmsDataService.getOwnersByAuthenticatedUser().then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(owners);
        });

        it("resets cache if data owner was deleted", () => {
            let retrievedFromApiCall: Pdms.DataOwner[];
            pdmsDataService.getOwnersByAuthenticatedUser().then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(owners);

            //  Delete owner.
            pdmsApiServiceMock.getFor("deleteDataOwnerById").and.returnValue(spec.asHttpPromise(null));
            pdmsDataService.deleteDataOwner(owner2);
            spec.runDigestCycle();

            retrievedFromApiCall = null;
            pdmsDataService.getOwnersByAuthenticatedUser().then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(owners);
        });
    });

    describe("createNewDataAgentInstance()", () => {
        it("successfully creates new delete data agent instance", () => {
            let result = pdmsDataService.createNewDataAgentInstance("delete-agent");

            expect(result).toBeDefined();
            expect(result.connectionDetails).toBeDefined();

            expect(result).toEqual({
                id: "",
                kind: "delete-agent",
                ownerId: "",
                sharingEnabled: false,
                isThirdPartyAgent: false,
                name: "",
                description: "",
                hasSharingRequests: false,
                connectionDetails: {
                    "PreProd": <Pdms.DataAgentConnectionDetails>{
                        protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                        authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth],
                        releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                        agentReadiness: Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd]
                    }
                },
                assetGroups: [],
                operationalReadiness: null,
                deploymentLocation: Pdms.PrivacyCloudInstanceId.Public,
                supportedClouds: [Pdms.PrivacyCloudInstanceId.Public],
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            });
        });

        it("throws, if unsupported data agent type was requested", () => {
            //  Completely unsupported kind.
            let unsupportedKind: any = "unsupported-kind";
            expect(() => pdmsDataService.createNewDataAgentInstance(unsupportedKind)).toThrowError();
        });
    });

    describe("resetDataAgentConnectionDetails()", () => {
        let testDataAgent: Pdms.DataAgent;

        beforeEach(() => {
            testDataAgent = pdmsDataService.createNewDataAgentInstance("delete-agent");
        });

        it("applies correct connection details for CommandFeedV1 protocol", () => {
            testDataAgent.connectionDetails[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].protocol = Pdms.PrivacyProtocolId.CommandFeedV1;
            pdmsDataService.resetDataAgentConnectionDetails(testDataAgent.connectionDetails[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]]);

            expect(testDataAgent.connectionDetails[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]]).toEqual(<Pdms.DataAgentConnectionDetails>{
                releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                agentReadiness: Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd],
                protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
                msaSiteId: 0,
                aadAppId: ""
            });
        });

        it("applies correct connection details for CosmosDeleteSignalV2 protocol", () => {
            testDataAgent.connectionDetails[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].protocol = Pdms.PrivacyProtocolId.CosmosDeleteSignalV2;
            pdmsDataService.resetDataAgentConnectionDetails(testDataAgent.connectionDetails[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]]);

            expect(testDataAgent.connectionDetails[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]]).toEqual(<Pdms.DataAgentConnectionDetails>{
                releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                agentReadiness: Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd],
                protocol: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2
            });
        });
    });

    it("gets asset group by ID", () => {
        let assetGroup: Pdms.AssetGroup = {
            id: "788897a2-193d-4537-93c1-3a56873305a0",
            ownerId: "78335f16-63dd-40db-9b6c-02fdc0cb9044",
            deleteAgentId: "5f912b78-89c1-45d8-9189-6f672c4758ff",
            optionalFeatures: ["MsaAgeOutOptIn"],
            qualifier: {
                props: {
                    AssetType: "33d80349-e0c8-44ce-ae2f-e2c8e16a3d33",
                    "prop1": "297073bd-2634-4101-ad77-90634e56d969"
                }
            }
        };
        pdmsApiServiceMock.getFor("getAssetGroupById").and.returnValue(spec.asHttpPromise(assetGroup));

        let retrievedFromApiCall: Pdms.AssetGroup;
        pdmsDataService.getAssetGroupById("788897a2-193d-4537-93c1-3a56873305a0").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(assetGroup);
    });

    it("gets asset groups by owner ID", () => {
        let assetGroup1: Pdms.AssetGroup = {
            id: "bce5b583-e5db-4563-b707-9db966ead0ae",
            ownerId: "5c8ec197-3efc-4de5-86e3-afd043728d91",
            deleteAgentId: "3bb3a10d-4676-4017-9a61-aef3af82ef35",
            optionalFeatures: ["MsaAgeOutOptIn"],
            qualifier: {
                props: {
                    AssetType: "4ab468d0-ccb3-4a09-abf7-ee49f7e8de47",
                    "prop1": "bff80467-5182-4f1a-9b94-4b339b90fb39"
                }
            }
        };
        let assetGroup2: Pdms.AssetGroup = {
            id: "4d3f0830-1966-41a0-bb2d-68a8204cd16e",
            ownerId: "5c8ec197-3efc-4de5-86e3-afd043728d91",
            deleteAgentId: "d74585be-fe20-42b5-a806-882114738453",
            optionalFeatures: null,
            qualifier: {
                props: {
                    AssetType: "1c4ff478-e667-42d2-b988-80dad7fcf505",
                    "prop1": "2d2db9b8-1743-4f98-bcec-40fcb42e170e"
                }
            }
        };

        let assetGroups = [assetGroup1, assetGroup2];
        pdmsApiServiceMock.getFor("getAssetGroupsByOwnerId").and.returnValue(spec.asHttpPromise(assetGroups));

        let retrievedFromApiCall: Pdms.AssetGroup[];
        pdmsDataService.getAssetGroupsByOwnerId("5c8ec197-3efc-4de5-86e3-afd043728d91").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(assetGroups);
    });

    it("gets asset groups count by owner ID", () => {
        let count = 10;
        pdmsApiServiceMock.getFor("getAssetGroupsCountByOwnerId").and.returnValue(spec.asHttpPromise(count));

        let retrievedFromApiCall: number;
        pdmsDataService.getAssetGroupsCountByOwnerId("5c8ec197-3efc-4de5-86e3-afd043728d91").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(count);
    });

    it("gets asset groups by delete agent ID", () => {
        let assetGroup1: Pdms.AssetGroup = {
            id: "881b5443-9a12-40c1-b94c-5c1bdbe45116",
            ownerId: "97916b94-9278-451f-9f81-585e1a42920f",
            deleteAgentId: "15fc71d5-cdcc-4fd5-a630-7ae3346e9f55",
            optionalFeatures: ["MsaAgeOutOptIn"],
            qualifier: {
                props: {
                    AssetType: "88c392a1-d32a-4638-a08e-e0c663d6a556",
                    "prop1": "9e4cd427-2711-462a-9b01-4c4ccf118709"
                }
            }
        };
        let assetGroup2: Pdms.AssetGroup = {
            id: "9c5fa198-6005-430d-ac9e-9e3393655095",
            ownerId: "bb32ee85-b58d-42c9-a97c-ed657d57b8a8",
            deleteAgentId: "15fc71d5-cdcc-4fd5-a630-7ae3346e9f55",
            optionalFeatures: null,
            qualifier: {
                props: {
                    AssetType: "7981a85e-63d3-4ee8-b9ac-efb0bf482b2d",
                    "prop1": "c2e29eea-0cd5-424b-8923-ac981f64843a"
                }
            }
        };

        let assetGroups = [assetGroup1, assetGroup2];
        pdmsApiServiceMock.getFor("getAssetGroupsByDeleteAgentId").and.returnValue(spec.asHttpPromise(assetGroups));

        let retrievedFromApiCall: Pdms.AssetGroup[];
        pdmsDataService.getAssetGroupsByDeleteAgentId("15fc71d5-cdcc-4fd5-a630-7ae3346e9f55").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(assetGroups);
    });

    describe("getAssetGroupsForAgent", () => {
        let dataAgent: Pdms.DataAgent;
        let assetGroup: Pdms.AssetGroup;

        beforeEach(() => {
            dataAgent = {
                id: "anyAgentId",
                kind: "delete-agent",
                assetGroups: [],
                name: "anyName",
                description: "anyDescription",
                ownerId: "anyOwnerId",
                sharingEnabled: false,
                isThirdPartyAgent: false,
                hasSharingRequests: false,
                connectionDetails: {
                    prod: {
                        releaseState: "anyReleaseState",
                        protocol: "anyProtocol"
                    }
                },
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            };
            assetGroup = {
                id: "anyId",
                ownerId: null,
                deleteAgentId: null,
                exportAgentId: null,
                qualifier: {
                    props: {
                        AssetType: "anyAssetType"
                    }
                }
            };
        });

        it("gets assets linked only to delete agent", (done: DoneFn) => {
            assetGroup.ownerId = "anyOwnerId";
            assetGroup.deleteAgentId = "anyAgentId";
            pdmsApiServiceMock.getFor("getAssetGroupsByDeleteAgentId").and.returnValue(spec.asHttpPromise([assetGroup]));
            pdmsApiServiceMock.getFor("getAssetGroupsByOwnerId").and.returnValue(spec.asHttpPromise([]));

            pdmsDataService.getAssetGroupsForAgent(dataAgent, "anyOwnerId")
                .then((result: Pdms.DataAgentSupportedAssetGroups) => {
                    expect(result.linkedAssetGroups).toEqual([assetGroup]);
                    expect(result.unlinkedAssetGroups).toEqual([]);
                    done();
                });
            spec.runDigestCycle();
        });

        it("gets assets linked only to export agent", (done: DoneFn) => {
            assetGroup.ownerId = "anyOwnerId";
            assetGroup.exportAgentId = "anyAgentId";
            pdmsApiServiceMock.getFor("getAssetGroupsByDeleteAgentId").and.returnValue(spec.asHttpPromise([]));
            pdmsApiServiceMock.getFor("getAssetGroupsByOwnerId").and.returnValue(spec.asHttpPromise([assetGroup]));

            pdmsDataService.getAssetGroupsForAgent(dataAgent, "anyOwnerId")
                .then((result: Pdms.DataAgentSupportedAssetGroups) => {
                    expect(result.linkedAssetGroups).toEqual([assetGroup]);
                    expect(result.unlinkedAssetGroups).toEqual([]);
                    done();
                });
            spec.runDigestCycle();
        });

        it("gets unique linked assets", (done: DoneFn) => {
            assetGroup.ownerId = "anyOwnerId";
            assetGroup.deleteAgentId = "anyAgentId";
            assetGroup.exportAgentId = "anyAgentId";
            let ownedAssetGroup: Pdms.AssetGroup = {
                id: "anyOwnerId",
                ownerId: null,
                deleteAgentId: "otherAgentId",
                exportAgentId: null,
                qualifier: {
                    props: {
                        AssetType: "anyAssetType"
                    }
                }
            };
            let exportAssetGroup: Pdms.AssetGroup = {
                id: "anyOwnerId",
                ownerId: null,
                deleteAgentId: "otherAgentId",
                exportAgentId: "anyAgentId",
                qualifier: {
                    props: {
                        AssetType: "anyAssetType"
                    }
                }
            };
            pdmsApiServiceMock.getFor("getAssetGroupsByDeleteAgentId").and.returnValue(spec.asHttpPromise([assetGroup]));
            pdmsApiServiceMock.getFor("getAssetGroupsByOwnerId").and.returnValue(spec.asHttpPromise([assetGroup, ownedAssetGroup, exportAssetGroup]));

            pdmsDataService.getAssetGroupsForAgent(dataAgent, "anyOwnerId")
                .then((result: Pdms.DataAgentSupportedAssetGroups) => {
                    expect(result.linkedAssetGroups).toEqual([assetGroup, exportAssetGroup]);
                    expect(result.unlinkedAssetGroups).toEqual([ownedAssetGroup]);
                    done();
                });
            spec.runDigestCycle();
        });

        it("gets assets that are not linked to any agent", (done: DoneFn) => {
            assetGroup.ownerId = "anyOwnerId";
            pdmsApiServiceMock.getFor("getAssetGroupsByDeleteAgentId").and.returnValue(spec.asHttpPromise([]));
            pdmsApiServiceMock.getFor("getAssetGroupsByOwnerId").and.returnValue(spec.asHttpPromise([assetGroup]));

            pdmsDataService.getAssetGroupsForAgent(dataAgent, "anyOwnerId")
                .then((result: Pdms.DataAgentSupportedAssetGroups) => {
                    expect(result.linkedAssetGroups).toEqual([]);
                    expect(result.unlinkedAssetGroups).toEqual([assetGroup]);
                    done();
                });
            spec.runDigestCycle();
        });
    });

    it("gets variant definition by ID", () => {
        let variantDefinition: Pdms.VariantDefinition = {
            id: "anyId",
            name: "anyName",
            description: "anyDescription",
            ownerId: "anyOwnerId",
            approver: "anyApprover",
            capabilities: ["anyCap1"],
            dataTypes: ["anyDt1"],
            subjectTypes: ["anySt1"]
        };
        pdmsApiServiceMock.getFor("getVariantById").and.returnValue(spec.asHttpPromise(variantDefinition));

        let retrievedFromApiCall: Pdms.VariantDefinition;
        pdmsDataService.getVariantById("anyId").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(variantDefinition);
    });

    it("gets existing data agents by owner ID", () => {
        let dataAgent1: Pdms.DataAgent = {
            kind: "delete-agent",
            id: "2a5e2173-9d95-4384-9764-21fcc8d0ce60",
            ownerId: "c15822fd-cd43-4357-be63-6f955f08a1be",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            name: "a71adbba-3ace-4766-ad21-0323025d4ea4",
            description: "data agent description",
            hasSharingRequests: false,
            connectionDetails: {
                "PreProd": <Pdms.DataAgentConnectionDetails> {
                    protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth]
                }
            },
            assetGroups: [],
            operationalReadiness: {
                isLoggingEnabled: true,
                isLoggingCompliant: true,
                isLoggingIncludesCommandId: true,
                isReliabilityAlertsTrigger: true,
                isLatencyAlertsTrigger: true,
                isMonitoringSla: true,
                isScalableForCommandRate: true,
                isAlertsInIcm: true,
                isDriDocumentation: true,
                isDriEscalation: true,
                isIncidentSeverityDoc: true,
                isGuidesPublished: true,
                isE2eValidation: true,
                isCertExpiryAlerts: true,
                isCertChangeDoc: true,
                isServiceRecoveryPlan: true,
                isServiceInProd: true,
                isDisasterRecoveryPlan: true,
                isDisasterRecoveryTested: true,
            },
            deploymentLocation: null,
            supportedClouds: null,
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
        };
        let dataAgent2: Pdms.DataAgent = {
            kind: "delete-agent",
            id: "7a83eb36-9f34-4ace-9a2a-ee95fa8474fe",
            ownerId: "c15822fd-cd43-4357-be63-6f955f08a1be",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            name: "a1e1d797-c6f6-4883-b4ba-795880529730",
            description: "data agent description",
            hasSharingRequests: false,
            connectionDetails: {
                "PreProd": <Pdms.DataAgentConnectionDetails> {
                    protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth]
                }
            },
            assetGroups: [],
            operationalReadiness: {
                isLoggingEnabled: false,
                isLoggingCompliant: false,
                isLoggingIncludesCommandId: false,
                isReliabilityAlertsTrigger: false,
                isLatencyAlertsTrigger: false,
                isMonitoringSla: false,
                isScalableForCommandRate: false,
                isAlertsInIcm: false,
                isDriDocumentation: false,
                isDriEscalation: false,
                isIncidentSeverityDoc: false,
                isGuidesPublished: false,
                isE2eValidation: false,
                isCertExpiryAlerts: false,
                isCertChangeDoc: false,
                isServiceRecoveryPlan: false,
                isServiceInProd: false,
                isDisasterRecoveryPlan: false,
                isDisasterRecoveryTested: false,
            },
            deploymentLocation: null,
            supportedClouds: null,
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
        };

        let dataAgents = [dataAgent1, dataAgent2];
        pdmsApiServiceMock.getFor("getDataAgentsByOwnerId").and.returnValue(spec.asHttpPromise(dataAgents));

        let retrievedFromApiCall: Pdms.DataAgent[];
        pdmsDataService.getDataAgentsByOwnerId("c15822fd-cd43-4357-be63-6f955f08a1be").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(dataAgents);
    });

    describe("gets shared data agents", () => {

        let dataAgent1: Pdms.DataAgent;
        let dataAgent2: Pdms.DataAgent;

        beforeEach(() => {
            dataAgent1 = {
                kind: "delete-agent",
                id: "2a5e2173-9d95-4384-9764-21fcc8d0ce60",
                ownerId: "c15822fd-cd43-4357-be63-6f955f08a1be",
                sharingEnabled: true,
                isThirdPartyAgent: false,
                hasSharingRequests: false,
                name: "a71adbba-3ace-4766-ad21-0323025d4ea4",
                description: "data agent description",
                connectionDetails: {
                    "PreProd": <Pdms.DataAgentConnectionDetails> {
                        protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                        releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                        authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth]
                    }
                },
                assetGroups: [],
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            };
            dataAgent2 = {
                kind: "delete-agent",
                id: "7a83eb36-9f34-4ace-9a2a-ee95fa8474fe",
                ownerId: "c15822fd-cd43-4357-be63-6f955f08a1be",
                sharingEnabled: true,
                isThirdPartyAgent: false,
                hasSharingRequests: false,
                name: "a1e1d797-c6f6-4883-b4ba-795880529730",
                description: "data agent description",
                connectionDetails: {
                    "PreProd": <Pdms.DataAgentConnectionDetails> {
                        protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                        releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                        authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth]
                    }
                },
                assetGroups: [],
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            };
        });

        it("by owner ID", () => {
            let dataAgents = [dataAgent1, dataAgent2];
            pdmsApiServiceMock.getFor("getSharedDataAgentsByOwnerId").and.returnValue(spec.asHttpPromise(dataAgents));

            let retrievedFromApiCall: Pdms.DataAgent[];
            pdmsDataService.getSharedDataAgentsByOwnerId("c15822fd-cd43-4357-be63-6f955f08a1be").then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(dataAgents);
        });

        it("all", () => {
            let dataAgents = [dataAgent1, dataAgent2];
            pdmsApiServiceMock.getFor("getSharedDataAgents").and.returnValue(spec.asHttpPromise(dataAgents));

            let retrievedFromApiCall: Pdms.DataAgent[];
            pdmsDataService.getSharedDataAgents().then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(dataAgents);
        });

    });

    it("gets existing data owner with Service Tree", () => {
        let owner: Pdms.DataOwner = {
            id: "f874a862-34aa-4403-83c4-4951429c75a3",
            name: null,
            description: null,
            alertContacts: null,
            announcementContacts: null,
            sharingRequestContacts: [],
            assetGroups: null,
            dataAgents: null,
            writeSecurityGroups: [],
            serviceTree: {
                id: "any",
                kind: "serviceGroup"
            }
        };
        pdmsApiServiceMock.getFor("getDataOwnerWithServiceTree").and.returnValue(spec.asHttpPromise(owner));

        let retrievedFromApiCall: Pdms.DataOwner;
        pdmsDataService.getDataOwnerWithServiceTree("5570b8f7-5f02-4679-bc26-017ab7be859b").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(owner);
    });

    it("gets existing data owner by name", () => {
        let owner: Pdms.DataOwner = {
            id: "f874a862-34aa-4403-83c4-4951429c75a3",
            name: "5570b8f7-5f02-4679-bc26-017ab7be859b",
            description: "Described",
            alertContacts: [],
            announcementContacts: [],
            sharingRequestContacts: [],
            assetGroups: [],
            dataAgents: [],
            writeSecurityGroups: [],
            serviceTree: null
        };
        pdmsApiServiceMock.getFor("getDataOwnerByName").and.returnValue(spec.asHttpPromise(owner));

        let retrievedFromApiCall: Pdms.DataOwner;
        pdmsDataService.getDataOwnerByName("5570b8f7-5f02-4679-bc26-017ab7be859b").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(owner);
    });

    it("gets existing data owner names by substring", () => {
        let owners: Pdms.DataOwner[] = [{
            id: "f874a862-34aa-4403-83c4-4951429c75a3",
            name: "5570b8f7-5f02-4679-bc26-017ab7be859b",
            description: "Described",
            alertContacts: [],
            announcementContacts: [],
            sharingRequestContacts: [],
            assetGroups: [],
            dataAgents: [],
            writeSecurityGroups: [],
            serviceTree: null
        }];
        pdmsApiServiceMock.getFor("getDataOwnersBySubstring").and.returnValue(spec.asHttpPromise(owners));

        let retrievedFromApiCall: Pdms.DataOwner[];
        pdmsDataService.getDataOwnersBySubstring("owner").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(owners);
    });

    it("gets services by name substring", () => {
        let servicesFromSearch: Pdms.STServiceSearchResult[] = [
            {
                id: "555-GUID",
                kind: "service",
                name: "MEE Privacy Service"
            },
            {
                id: "555-GUID-1",
                kind: "teamGroup",
                name: "MEE Account Controls"
            }
        ];
        pdmsApiServiceMock.getFor("getServicesByName").and.returnValue(spec.asHttpPromise(servicesFromSearch));

        let retrievedFromApiCall: Pdms.STServiceSearchResult[];
        pdmsDataService.getServicesByName("MEE").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(servicesFromSearch);
    });

    it("gets service by id", () => {
        let serviceDetails: Pdms.STServiceDetails = {
            id: "555-GUID",
            name: "MEE Privacy Service",
            description: "For privacy.",
            serviceAdmins: ["alias1", "alias2"],
            organizationId: "OrgID",
            divisionId: "DivID",
            kind: "serviceGroup"
        };
        pdmsApiServiceMock.getFor("getServiceById").and.returnValue(spec.asHttpPromise(serviceDetails));

        let retrievedFromApiCall: Pdms.STServiceDetails;
        pdmsDataService.getServiceById("555-GUID", "serviceGroup").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(serviceDetails);
    });

    it("deletes existing data owner by id", () => {
        let owner: Pdms.DataOwner = {
            id: "c2197a09-f9e5-457c-b3f9-8200119c925f",
            name: "8b670a08-3221-4eb6-8d95-38b7d17bbf21",
            description: "Described",
            alertContacts: [],
            announcementContacts: [],
            sharingRequestContacts: [],
            assetGroups: [],
            dataAgents: [],
            writeSecurityGroups: [],
            serviceTree: null
        };
        pdmsApiServiceMock.getFor("deleteDataOwnerById").and.returnValue(spec.asHttpPromise(null));

        pdmsDataService.deleteDataOwner(owner);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("deleteDataOwnerById")).toHaveBeenCalledWith("c2197a09-f9e5-457c-b3f9-8200119c925f");
    });

    it("deletes existing data agent by id", () => {
        let dataAgent: Pdms.DeleteAgent = {
            id: "b7cdf7e4-94dc-48b5-9ab7-5286072d096d",
            kind: "delete-agent",
            ownerId: "c64b09d2-7a66-472e-9106-d42a85b8575d",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            hasSharingRequests: false,
            name: "updated data agent name",
            description: "updated data agent description",
            connectionDetails: {
                "PreProd": {
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                    authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
                    msaSiteId: 1
                }
            },
            assetGroups: [],
            operationalReadiness: null,
            deploymentLocation: null,
            supportedClouds: null,
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
        };

        pdmsApiServiceMock.getFor("deleteDataAgentById").and.returnValue(spec.asHttpPromise(dataAgent));

        pdmsDataService.deleteDataAgent(dataAgent);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("deleteDataAgentById")).toHaveBeenCalledWith("b7cdf7e4-94dc-48b5-9ab7-5286072d096d", false);
    });

    it("deletes existing data agent by id with override pending commands", () => {
        let dataAgent: Pdms.DeleteAgent = {
            id: "7eb00a70-aee2-4805-a4f7-70b1b458e2e2",
            kind: "delete-agent",
            ownerId: "c64b09d2-7a66-472e-9106-d42a85b8575d",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            hasSharingRequests: false,
            name: "updated data agent name",
            description: "updated data agent description",
            connectionDetails: {
                "PreProd": {
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                    authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
                    msaSiteId: 1
                }
            },
            assetGroups: [],
            operationalReadiness: null,
            deploymentLocation: null,
            supportedClouds: null,
            pendingCommandsFound: true,
            dataResidencyBoundary: "Global"
        };

        pdmsApiServiceMock.getFor("deleteDataAgentById").and.returnValue(spec.asHttpPromise(dataAgent));

        pdmsDataService.deleteDataAgent(dataAgent);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("deleteDataAgentById")).toHaveBeenCalledWith("7eb00a70-aee2-4805-a4f7-70b1b458e2e2", true);
    });

    it("deletes existing asset group by id", () => {
        let assetGroup: Pdms.AssetGroup = {
            id: "1a8bbf43-05f8-49f7-a0ce-7fd990e9885c",
            ownerId: null,
            deleteAgentId: "2ee707cb-e8a8-4224-a1e6-25101c0ce33f",
            exportAgentId: null,
            qualifier: {
                props: {
                    AssetType: "anyAssetType"
                }
            }
        };
        pdmsApiServiceMock.getFor("deleteAssetGroupById").and.stub();

        pdmsDataService.deleteAssetGroup(assetGroup);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("deleteAssetGroupById")).toHaveBeenCalledWith("1a8bbf43-05f8-49f7-a0ce-7fd990e9885c");
    });

    it("sets agent relationship with asset groups", () => {
        let request: SetAgentRelationshipRequest = {
            relationships: []
        };
        pdmsApiServiceMock.getFor("setAgentRelationshipsAsync").and.returnValue(spec.asHttpPromise(null));

        pdmsDataService.setAgentRelationshipsAsync(request);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("setAgentRelationshipsAsync")).toHaveBeenCalledWith(request);
    });

    it("gets a sharing request by id", () => {
        let request: Pdms.SharingRequest = {
            id: "9c5f8fe0-619a-434b-806f-46276ae13de1",
            agentId: "2c22345gh-8f454",
            ownerId: "4cb-44be-ac24-0114de4fcf07",
            ownerName: "OwnerName",
            relationships: [{
                assetGroupId: "1234567890a",
                assetGroupQualifier: {
                    props: {
                        AssetType: "CosmosStructuredStream",
                        PhysicalCluster: "guess",
                        VirtualCluster: "mylove",
                        RelativePath: "/local/lul"
                    }
                },
                capabilities: ["Delete", "Export"]
            }]
        };

        pdmsApiServiceMock.getFor("getSharingRequestById").and.returnValue(spec.asHttpPromise(request));

        let retrievedFromApiCall: Pdms.SharingRequest;
        pdmsDataService.getSharingRequestById("2c22345gh-8f454").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(request);
    });

    it("gets sharing requests by agent id", () => {
        let requests: Pdms.SharingRequest[] = [{
            id: "9c5f8fe0-619a-434b-806f-46276ae13de1",
            agentId: "2c22345gh-8f454",
            ownerId: "4cb-44be-ac24-0114de4fcf07",
            ownerName: "OwnerName",
            relationships: [{
                assetGroupId: "1234567890a",
                assetGroupQualifier: {
                    props: {
                        AssetType: "CosmosStructuredStream",
                        PhysicalCluster: "guess",
                        VirtualCluster: "mylove",
                        RelativePath: "/local/lul"
                    }
                },
                capabilities: ["Delete", "Export"]
            }]
        }, {
            id: "9c5f8fe0-619a-434b-806f-423gf7789",
            agentId: "2c22345gh-8f454",
            ownerId: "4cb-44be-ac24-0114de4fcf07",
            ownerName: "OwnerName2",
            relationships: [{
                assetGroupId: "1234567890avbc",
                assetGroupQualifier: {
                    props: {
                        AssetType: "CosmosStructuredStream",
                        PhysicalCluster: "guesses",
                        VirtualCluster: "myloves",
                        RelativePath: "/local/lulz"
                    }
                },
                capabilities: ["Delete", "Export"]
            }]
        }];

        pdmsApiServiceMock.getFor("getSharingRequestsByAgentId").and.returnValue(spec.asHttpPromise(requests));

        let retrievedFromApiCall: Pdms.SharingRequest[];
        pdmsDataService.getSharingRequestsByAgentId("2c22345gh-8f454").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(requests);
    });

    it("approves sharing requests by a list of ids", () => {
        let sharingRequestIds: string[] = ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"];

        pdmsApiServiceMock.getFor("approveSharingRequests").and.returnValue(spec.asHttpPromise(null));

        pdmsDataService.approveSharingRequests(sharingRequestIds);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("approveSharingRequests")).toHaveBeenCalledWith(sharingRequestIds);
    });

    it("denies sharing requests by a list of ids", () => {
        let sharingRequestIds: string[] = ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"];

        pdmsApiServiceMock.getFor("denySharingRequests").and.returnValue(spec.asHttpPromise(null));

        pdmsDataService.denySharingRequests(sharingRequestIds);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("denySharingRequests")).toHaveBeenCalledWith(sharingRequestIds);
    });

    it("creates a transfer request on asset groups between two owners", () => {
        let request: Pdms.TransferRequest = {
            id: null,
            sourceOwnerId: "9c5f8fe0-619a-434b-806f-46276ae13de1",
            sourceOwnerName: null,
            targetOwnerId: "9c5f8fe0-619a-434b-806f-423gf7789",
            requestState: Pdms.TransferRequestState.None,
            assetGroups: []
        };
        pdmsApiServiceMock.getFor("createTransferRequest").and.returnValue(spec.asHttpPromise(null));

        pdmsDataService.createTransferRequest(request);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("createTransferRequest")).toHaveBeenCalledWith(request);
    });

    it("gets transfer requests by target owner id", () => {
        let requests: Pdms.TransferRequest[] = [{
            id: null,
            sourceOwnerId: "9c5f8fe0-619a-434b-806f-46276ae13de1",
            sourceOwnerName: null,
            targetOwnerId: "9c5f8fe0-619a-434b-806f-423gf7789",
            requestState: Pdms.TransferRequestState.None,
            assetGroups: []
        }, {
            id: "9c5f8fe0-619a-434b-806f-423gf7789",
            sourceOwnerId: "9c5f8fe0-619a-434b-806f-46276ae13de1",
            sourceOwnerName: null,
            targetOwnerId: "9c5f8fe0-619a-434b-806f-423gf7789",
            requestState: Pdms.TransferRequestState.None,
            assetGroups: []
        }];

        pdmsApiServiceMock.getFor("getTransferRequestsByTargetOwnerId").and.returnValue(spec.asHttpPromise(requests));

        let retrievedFromApiCall: Pdms.TransferRequest[];
        pdmsDataService.getTransferRequestsByTargetOwnerId("2c22345gh-8f454").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(requests);
    });

    it("approves transfer requests by a list of ids", () => {
        let transferRequestIds: string[] = ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"];

        pdmsApiServiceMock.getFor("approveTransferRequests").and.returnValue(spec.asHttpPromise(null));

        pdmsDataService.approveTransferRequests(transferRequestIds);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("approveTransferRequests")).toHaveBeenCalledWith(transferRequestIds);
    });

    it("denies transfer requests by a list of ids", () => {
        let transferRequestIds: string[] = ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"];

        pdmsApiServiceMock.getFor("denyTransferRequests").and.returnValue(spec.asHttpPromise(null));

        pdmsDataService.denyTransferRequests(transferRequestIds);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("denyTransferRequests")).toHaveBeenCalledWith(transferRequestIds);
    });

    it("gets search results", () => {
        let searchResults: SearchTypes.SearchResults = {};
        pdmsApiServiceMock.getFor("search").and.returnValue(spec.asHttpPromise(searchResults));

        let retrievedFromApiCall: SearchTypes.SearchResults;
        pdmsDataService.search("search terms").then(data => retrievedFromApiCall = data);
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(searchResults);
    });

    it("creates an icm incident", () => {
        pdmsApiServiceMock.getFor("createIcmIncident").and.returnValue(spec.asHttpPromise(null));
        let incident: Pdms.Incident = {
            routing: null,
            severity: Pdms.IcmIncidentSeverity.Sev4,
            title: "Agent IcM Ticket on PCD",
            body: "Blah",
            keywords: null
        };

        pdmsDataService.createIcmIncident(incident);
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("createIcmIncident")).toHaveBeenCalledWith(incident);
    });

    it("has authorization for incident managers", () => {
        pdmsApiServiceMock.getFor("hasAccessForIncidentManager").and.returnValue(spec.asHttpPromise(null));

        pdmsDataService.hasAccessForIncidentManager();
        spec.runDigestCycle();
        expect(pdmsApiServiceMock.getFor("hasAccessForIncidentManager")).toHaveBeenCalled();
    });
});
