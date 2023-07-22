import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { IAjaxService } from "../ajax.service";

import * as Pdms from "./pdms-types";
import { IPdmsApiService } from "./pdms-api.service";

//  Load code under test.
import "./pdms-api.service";
import { Capability, SetAgentRelationshipRequest } from "../pdms-agent-relationship-types";

describe("PDMS API service", () => {
    let pdmsApiService: IPdmsApiService;
    let ajaxServiceMock: SpyCache<IAjaxService>;

    beforeEach(() => {
        let spec = new TestSpec({
            returnMockedAjaxService: true,
            mockedAjaxServiceOptions: {
                authTokenManager: null
            }
        });
        ajaxServiceMock = spec.ajaxServiceMock;

        inject((_pdmsApiService_: IPdmsApiService) => {
            pdmsApiService = _pdmsApiService_;
        });
    });

    it("gets owners by the authenticated user", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getOwnersByAuthenticatedUser();
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getownersbyauthenticateduser",
            serviceName: "PdmsUx",
            operationName: "GetOwnersByAuthenticatedUser",
        });
    });

    it("creates data owner", () => {
        ajaxServiceMock.getFor("put").and.stub();

        let dataOwner: Pdms.DataOwner = {
            id: "",
            name: "data owner name",
            description: "data owner description",
            alertContacts: ["contact1", "contact2"],
            announcementContacts: ["contact3", "contact4"],
            sharingRequestContacts: [],
            assetGroups: [],
            dataAgents: [],
            writeSecurityGroups: ["group1", "group2"],
            tagSecurityGroups: ["group1"],
            serviceTree: null
        };
        pdmsApiService.createDataOwner(dataOwner);

        expect(ajaxServiceMock.getFor("put")).toHaveBeenCalledWith({
            url: "/api/createdataowner",
            serviceName: "PdmsUx",
            operationName: "CreateDataOwner",
            data: dataOwner
        });
    });

    it("creates data owner by service tree id", () => {
        ajaxServiceMock.getFor("put").and.stub();

        let serviceTreeOwner: Pdms.STDataOwner = {
            id: null,
            serviceTreeId: "any",
            serviceTreeIdKind: "service",
            writeSecurityGroups: ["anySG1", "anySG2"],
            tagSecurityGroups: ["anySG1"],
            tagApplicationIds: ["8p2jeaf3-fca2-483b-a542-3e6e99bcd2f2"],
            sharingRequestContacts: []
        };
        pdmsApiService.createDataOwnerWithServiceTree(serviceTreeOwner);

        expect(ajaxServiceMock.getFor("put")).toHaveBeenCalledWith({
            url: "/api/createdataownerwithservicetree",
            serviceName: "PdmsUx",
            operationName: "CreateDataOwnerWithServiceTree",
            data: serviceTreeOwner
        });
    });

    it("updates data owner", () => {
        ajaxServiceMock.getFor("post").and.stub();

        let dataOwner: Pdms.DataOwner = {
            id: "5e5eeaf3-fca2-483b-a542-3e6e99bcd2f2",
            name: "updated data owner name",
            description: "updated data owner description",
            alertContacts: ["updated contact1", "updated contact2"],
            announcementContacts: ["updated contact3", "updated contact4"],
            sharingRequestContacts: [],
            assetGroups: [],
            dataAgents: [],
            writeSecurityGroups: ["updated group1", "updated group2"],
            tagSecurityGroups: ["updated group1"],
            serviceTree: null
        };
        pdmsApiService.updateDataOwner(dataOwner);

        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/api/updatedataowner",
            serviceName: "PdmsUx",
            operationName: "UpdateDataOwner",
            data: dataOwner
        });
    });

    it("updates data owner with Service Tree", () => {
        ajaxServiceMock.getFor("post").and.stub();

        let serviceTreeOwner: Pdms.STDataOwner = {
            id: "5e5eeaf3-fca2-483b-a542-3e6e99bcd2f2",
            serviceTreeId: "any",
            serviceTreeIdKind: "service",
            writeSecurityGroups: ["updated group1", "updated group2"],
            tagSecurityGroups: [],
            tagApplicationIds: ["8p2jeaf3-fca2-483b-a542-3e6e99bcd2f2"],
            sharingRequestContacts: []
        };
        pdmsApiService.updateDataOwnerWithServiceTree(serviceTreeOwner);

        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/api/updatedataownerwithservicetree",
            serviceName: "PdmsUx",
            operationName: "UpdateDataOwnerWithServiceTree",
            data: serviceTreeOwner
        });
    });

    it("links data owner with Service Tree", () => {
        ajaxServiceMock.getFor("post").and.stub();

        let serviceTreeEntity: Pdms.STEntityBase = {
            id: "0d038926-47f6-43ea-a75a-a5ae84a849c9",
            kind: "teamGroup",
        };
        pdmsApiService.linkDataOwnerToServiceTree("ff225970-6777-4fee-ac39-0430d2f0adae", serviceTreeEntity);

        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/api/linkdataownertoservicetree?id=ff225970-6777-4fee-ac39-0430d2f0adae&serviceTreeId=0d038926-47f6-43ea-a75a-a5ae84a849c9&serviceTreeIdKind=teamGroup",
            serviceName: "PdmsUx",
            operationName: "LinkDataOwnerToServiceTree"
        });
    });

    it("deletes existing data owner", () => {
        ajaxServiceMock.getFor("del").and.stub();

        pdmsApiService.deleteDataOwnerById("C5CE2637-D6F9-44A5-8F6A-4253C3850217");

        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "/api/deletedataownerbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteDataOwnerById",
            data: { id: "C5CE2637-D6F9-44A5-8F6A-4253C3850217" }
        });
    });

    it("gets privacy policy", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getPrivacyPolicy();

        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getprivacypolicy",
            serviceName: "PdmsUx",
            operationName: "GetPrivacyPolicy"
        });
    });

    it("gets data assets based on asset group qualifier", () => {
        // arrange
        let assetGroupQualifier: Pdms.AssetGroupQualifier = {
            props: {
                AssetType: "AssetType1"
            }
        };
        ajaxServiceMock.getFor("get").and.stub();

        // act
        pdmsApiService.getDataAssetsByAssetGroupQualifier(assetGroupQualifier);

        // assert
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getdataassetsbyassetgroupqualifier",
            serviceName: "PdmsUx",
            operationName: "GetDataAssetsByAssetGroupQualifier",
            data: { assetQualifierJson: JSON.stringify(assetGroupQualifier) }
        });
    });

    it("gets existing delete data agent", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getDeleteAgentById("46288429-702d-4239-b970-33555884f1f9");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getdeleteagentbyid",
            serviceName: "PdmsUx",
            operationName: "GetDeleteAgentById",
            data: { id: "46288429-702d-4239-b970-33555884f1f9" }
        });
    });

    it("creates delete data agent", () => {
        ajaxServiceMock.getFor("put").and.stub();

        let dataAgent: Pdms.DeleteAgent = {
            id: "",
            kind: "delete-agent",
            ownerId: "8b376a59-6396-4cc7-b5c9-03478634c943",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            hasSharingRequests: false,
            name: "data agent name",
            description: "data agent description",
            connectionDetails: {
                "PreProd": <Pdms.DataAgentConnectionDetails> {
                    protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
                    msaSiteId: 2
                }
            },
            assetGroups: [],
            operationalReadiness: null,
            deploymentLocation: null,
            supportedClouds: null,
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
        };
        pdmsApiService.createDeleteAgent(dataAgent);

        expect(ajaxServiceMock.getFor("put")).toHaveBeenCalledWith({
            url: "/api/createdeleteagent",
            serviceName: "PdmsUx",
            operationName: "CreateDeleteAgent",
            data: dataAgent
        });
    });

    it("updates delete data agent", () => {
        ajaxServiceMock.getFor("post").and.stub();

        let dataAgent: Pdms.DeleteAgent = {
            id: "e6af765c-a755-4639-afa5-9dc26a3836c8",
            kind: "delete-agent",
            ownerId: "bbead734-edee-4329-b010-d088b12279f7",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            hasSharingRequests: false,
            name: "updated data agent name",
            description: "updated data agent description",
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
        pdmsApiService.updateDeleteAgent(dataAgent);

        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/api/updatedeleteagent",
            serviceName: "PdmsUx",
            operationName: "UpdateDeleteAgent",
            dataType: "json",
            data: dataAgent
        });
    });

    it("deletes existing data agent", () => {
        ajaxServiceMock.getFor("del").and.stub();

        pdmsApiService.deleteDataAgentById("7362FCB9-6BE6-4BD1-9255-79B3F2195BE8", false);

        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "/api/deletedataagentbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteDataAgentById",
            data: { id: "7362FCB9-6BE6-4BD1-9255-79B3F2195BE8", overridePendingCommands: false}
        });
    });

    it("deletes existing data agent with override pending commands", () => {
        ajaxServiceMock.getFor("del").and.stub();

        pdmsApiService.deleteDataAgentById("7368D478-E2A3-483D-A286-B7BB669F7D00", true);

        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "/api/deletedataagentbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteDataAgentById",
            data: { id: "7368D478-E2A3-483D-A286-B7BB669F7D00", overridePendingCommands: true }
        });
    });

    it("gets asset type metadata", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getAssetTypeMetadata();
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getassettypemetadata",
            serviceName: "PdmsUx",
            operationName: "GetAssetTypeMetadata"
        });
    });

    it("gets asset group by ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getAssetGroupById("64718259-57a3-479a-a636-ae94e5ba07ae");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getassetgroupbyid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupById",
            data: { id: "64718259-57a3-479a-a636-ae94e5ba07ae" }
        });
    });

    it("gets asset groups by owner ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getAssetGroupsByOwnerId("f9232948-470a-42f1-ab16-e611cc1b98c9");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getassetgroupsbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupsByOwnerId",
            data: { ownerId: "f9232948-470a-42f1-ab16-e611cc1b98c9" }
        });
    });

    it("gets asset groups count by owner ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getAssetGroupsCountByOwnerId("f9232948-470a-42f1-ab16-e611cc1b98c9");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getassetgroupscountbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupsCountByOwnerId",
            data: { ownerId: "f9232948-470a-42f1-ab16-e611cc1b98c9" }
        });
    });

    it("gets asset groups by delete agent ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getAssetGroupsByDeleteAgentId("f2716f24-fd7f-4a71-a73c-9d4bb58b3e28");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getassetgroupsbydeleteagentid",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupsByDeleteAgentId",
            data: { deleteAgentId: "f2716f24-fd7f-4a71-a73c-9d4bb58b3e28" }
        });
    });

    it("deletes existing asset group", () => {
        ajaxServiceMock.getFor("del").and.stub();

        pdmsApiService.deleteAssetGroupById("D87D25BD-4448-4E17-AC98-44C4A57802FB");

        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "/api/deleteassetgroupbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteAssetGroupById",
            data: { id: "D87D25BD-4448-4E17-AC98-44C4A57802FB" }
        });
    });

    it("gets existing data agents by owner ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getDataAgentsByOwnerId("3d9e8469-669b-4146-b8c0-c7f40d82e4ca");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getdataagentsbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetDataAgentsByOwnerId",
            data: { ownerId: "3d9e8469-669b-4146-b8c0-c7f40d82e4ca" }
        });
    });

    it("gets shared data agents by owner ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getSharedDataAgentsByOwnerId("3d9e8469-669b-4146-b8c0-c7f40d82e4ca");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getshareddataagentsbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetSharedDataAgentsByOwnerId",
            data: { ownerId: "3d9e8469-669b-4146-b8c0-c7f40d82e4ca" }
        });
    });

    it("gets shared data agents", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getSharedDataAgents();
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getshareddataagents",
            serviceName: "PdmsUx",
            operationName: "GetSharedDataAgents",
            data: { }
        });
    });

    it("gets services by name", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getServicesByName("MEE");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getservicesbyname",
            serviceName: "PdmsUx",
            operationName: "GetServicesByName",
            data: { nameSubstring: "MEE" }
        });
    });

    it("gets service by ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getServiceById("555-GUID-555", "teamGroup");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getservicebyid",
            serviceName: "PdmsUx",
            operationName: "GetServiceById",
            data: { id: "555-GUID-555", kind: "teamGroup" }
        });
    });

    it("gets existing data owner with Service Tree", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getDataOwnerWithServiceTree("a7a80139-82a1-49b8-a049-27d5e567223e");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getdataownerwithservicetree",
            serviceName: "PdmsUx",
            operationName: "GetDataOwnerWithServiceTree",
            data: { id: "a7a80139-82a1-49b8-a049-27d5e567223e" }
        });
    });

    it("gets existing data owner by name", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getDataOwnerByName("a7a80139-82a1-49b8-a049-27d5e567223e");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getdataownerbyname",
            serviceName: "PdmsUx",
            operationName: "GetDataOwnerByName",
            data: { ownerName: "a7a80139-82a1-49b8-a049-27d5e567223e" }
        });
    });

    it("gets existing data owner names by substring", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getDataOwnersBySubstring("2a906b91-d4fa-4e46-9eea-8a757e8007f1");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getdataownersbysubstring",
            serviceName: "PdmsUx",
            operationName: "GetDataOwnersBySubstring",
            data: { ownerSubstring: "2a906b91-d4fa-4e46-9eea-8a757e8007f1" }
        });
    });

    it("sets agent relationship with asset groups", () => {
        ajaxServiceMock.getFor("post").and.stub();

        let request: SetAgentRelationshipRequest = {
            relationships : []
        };

        pdmsApiService.setAgentRelationshipsAsync(request);

        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/api/setagentrelationships",
            serviceName: "PdmsUx",
            operationName: "SetAgentRelationships",
            data: request
        });
    });

    it("gets a sharing request by ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getSharingRequestById("2c22345gh-8f45409");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getsharingrequestbyid",
            serviceName: "PdmsUx",
            operationName: "GetSharingRequestById",
            data: { id: "2c22345gh-8f45409" }
        });
    });

    it("gets sharing requests by agent ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getSharingRequestsByAgentId("2c22345gh-8f454");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getsharingrequestsbyagentid",
            serviceName: "PdmsUx",
            operationName: "GetSharingRequestsByAgentId",
            data: { agentId: "2c22345gh-8f454" }
        });
    });

    it("approve sharing requests by a list of IDs", () => {
        let sharingRequestIds: string[] = ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"];
        ajaxServiceMock.getFor("post").and.stub();

        pdmsApiService.approveSharingRequests(sharingRequestIds);
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/api/approvesharingrequests",
            serviceName: "PdmsUx",
            operationName: "ApproveSharingRequests",
            data: ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"]
        });
    });

    it("deny sharing requests by a list of IDs", () => {
        let sharingRequestIds: string[] = ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"];
        ajaxServiceMock.getFor("del").and.stub();

        pdmsApiService.denySharingRequests(sharingRequestIds);
        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "/api/denysharingrequests",
            serviceName: "PdmsUx",
            operationName: "DenySharingRequests",
            data: { sharingRequestIds: ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"] }
        });
    });

    it("creates a transfer request on asset groups between two owners", () => {
        ajaxServiceMock.getFor("post").and.stub();

        let request: Pdms.TransferRequest = {
            id: null,
            sourceOwnerId: "9c5f8fe0-619a-434b-806f-46276ae13de1",
            sourceOwnerName: null,
            targetOwnerId: "9c5f8fe0-619a-434b-806f-423gf7789",
            requestState: Pdms.TransferRequestState.None,
            assetGroups: []
        };

        pdmsApiService.createTransferRequest(request);

        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/asset-transfer-request/api/createtransferrequest",
            serviceName: "PdmsUx",
            operationName: "CreateTransferRequest",
            data: request
        });
    });

    it("gets transfer requests by target owner ID", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.getTransferRequestsByTargetOwnerId("2c22345gh-8f454");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/asset-transfer-request/api/gettransferrequestsbytargetownerid",
            serviceName: "PdmsUx",
            operationName: "GetTransferRequestsByTargetOwnerId",
            data: { ownerId: "2c22345gh-8f454" }
        });
    });

    it("approve transfer requests by a list of IDs", () => {
        let transferRequestIds: string[] = ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"];
        ajaxServiceMock.getFor("post").and.stub();

        pdmsApiService.approveTransferRequests(transferRequestIds);
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/asset-transfer-request/api/approvetransferrequests",
            serviceName: "PdmsUx",
            operationName: "ApproveTransferRequests",
            data: ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"]
        });
    });

    it("deny transfer requests by a list of IDs", () => {
        let transferRequestIds: string[] = ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"];
        ajaxServiceMock.getFor("del").and.stub();

        pdmsApiService.denyTransferRequests(transferRequestIds);
        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "/asset-transfer-request/api/denytransferrequests",
            serviceName: "PdmsUx",
            operationName: "DenyTransferRequests",
            data: { transferRequestIds: ["9c5f8fe0-619a-434b-806f-46276ae13de1", "9c5f8fe0-619a-434b-806f-423gf7789"] }
        });
    });

    it("gets search results", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.search("search terms");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/search",
            serviceName: "PdmsUx",
            operationName: "Search",
            data: { terms: "search terms" },
            cache: true
        });
    });

    it("creates an icm incident on data agent owners with default title and body", () => {
        ajaxServiceMock.getFor("post").and.stub();
        let incident: Pdms.Incident = {
            routing: {
                ownerId: "123456789",
                agentId: "0987654321"
            },
            severity: Pdms.IcmIncidentSeverity.Sev4,
            title: "Data agent export failed validation",
            body: "",
            keywords: null
        };

        pdmsApiService.createIcmIncident(incident);
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/api/createicmincident",
            serviceName: "PdmsUx",
            operationName: "CreateIcmIncident",
            data: incident
        });
    });
    
    it("checks if user has authorization for incident managers", () => {
        ajaxServiceMock.getFor("get").and.stub();

        pdmsApiService.hasAccessForIncidentManager();
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/hasaccessforincidentmanager",
            serviceName: "PdmsUx",
            operationName: "HasAccessForIncidentManager"
        });
    });
});
