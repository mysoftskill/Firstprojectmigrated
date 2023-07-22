import { TestSpec, ComponentInstance, SpyCache } from "../../../shared-tests/spec.base";
import DefaultPageContextComponent from "./default-page-context.component";

import * as Pdms from "../../../shared/pdms/pdms-types";
import { VariantRequest } from "../../../shared/variant/variant-types";

interface ComponentParams {
    ownerId?: string;
    agentId?: string;
    assetGroupId?: string;
    variantRequestId?: string;
    owner?: Pdms.DataOwner;
    agent?: Pdms.DeleteAgent;
    assetGroup?: Pdms.AssetGroup;
    variantRequest?: VariantRequest;
}

describe("Default page context", () => {
    let spec: TestSpec;
    let owner: Pdms.DataOwner;
    let agent: Pdms.DeleteAgent;
    let assetGroup: Pdms.AssetGroup;
    let variantRequest: VariantRequest;

    beforeAll(() => {
        owner = {
            id: "specificOwnerId",
            name: null,
            description: null,
            alertContacts: null,
            announcementContacts: null,
            sharingRequestContacts: null,
            assetGroups: null,
            dataAgents: null,
            writeSecurityGroups: null,
            serviceTree: null
        };
        agent = {
            kind: null,
            assetGroups: null,
            id: "specificAgentId",
            name: null,
            description: null,
            ownerId: "specificOwnerId",
            sharingEnabled: null,
            isThirdPartyAgent: null,
            connectionDetails: null,
            operationalReadiness: null,
            deploymentLocation: null,
            supportedClouds: null,
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
        };
        assetGroup = {
            id: "specificAssetGroupId",
            ownerId: "specificOwnerId",
            qualifier: null,
        };
        variantRequest = {
            id: "specificVariantRequestId",
            ownerId: "specificOwnerId",
            ownerName: null,
            trackingDetails: null,
            requestedVariants: null,
            variantRelationships: null,
        };
    });

    beforeEach(() => {
        spec = new TestSpec();
    });

    describe(("does the appropriate entity lookup"), () => {
        it(("owner ID"), () => {
            // arrange
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
            let component = createComponent({ ownerId: "specificOwnerId" });

            // act
            component.instance.getPageContextData();

            // assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree")).toHaveBeenCalledWith("specificOwnerId");
        });

        it(("agent ID"), () => {
            // arrange
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDeleteAgentById", agent);
            let component = createComponent({ agentId: "specificAgentId" });

            // act
            component.instance.getPageContextData();

            // assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDeleteAgentById")).toHaveBeenCalled();
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree")).toHaveBeenCalledWith(agent.ownerId);
        });

        it(("asset group ID"), () => {
            // arrange
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getAssetGroupById", assetGroup);
            let component = createComponent({ assetGroupId: "specificAssetGroupId" });

            // act
            component.instance.getPageContextData();

            // assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getAssetGroupById")).toHaveBeenCalled();
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree")).toHaveBeenCalledWith(assetGroup.ownerId);
        });

        it(("variant request ID"), () => {
            // arrange
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
            spec.dataServiceMocks.variantDataService.mockAsyncResultOf("getVariantRequestById", variantRequest);
            let component = createComponent({ variantRequestId: "specificVariantRequestId" });

            // act
            component.instance.getPageContextData();

            // assert
            expect(spec.dataServiceMocks.variantDataService.getFor("getVariantRequestById")).toHaveBeenCalled();
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree")).toHaveBeenCalledWith(variantRequest.ownerId);
        });
    });

    describe(("does the appropriate entity lookup with entities bound"), () => {
        it(("owner"), () => {
            // arrange
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree");
            let component = createComponent({ owner });

            // act
            component.instance.getPageContextData();

            // assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree")).not.toHaveBeenCalled();
        });

        it(("agent"), () => {
            // arrange
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
            let component = createComponent({ agent });

            // act
            component.instance.getPageContextData();

            // assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree")).toHaveBeenCalledWith(agent.ownerId);
        });

        it(("asset group"), () => {
            // arrange
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
            let component = createComponent({ assetGroup });

            // act
            component.instance.getPageContextData();

            // assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree")).toHaveBeenCalledWith(assetGroup.ownerId);
        });

        it(("variant request"), () => {
            // arrange
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
            let component = createComponent({ variantRequest });

            // act
            component.instance.getPageContextData();

            // assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree")).toHaveBeenCalledWith(variantRequest.ownerId);
        });
    });

    describe("will display enough entities for context", () => {
        beforeEach(() => {
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDeleteAgentById", agent);
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getAssetGroupById", assetGroup);
            spec.dataServiceMocks.variantDataService.mockAsyncResultOf("getVariantRequestById", variantRequest);
        });

        it("for an owner", () => {
            // arrange
            let component = createComponent({ owner });

            // act
            component.instance.getPageContextData();

            // assert
            expect(component.instance.owner).toBeTruthy();
            expect(component.instance.agent).toBeFalsy();
            expect(component.instance.assetGroup).toBeFalsy();
        });

        it("for an agent", () => {
            // arrange
            let component = createComponent({ agent });

            // act
            component.instance.getPageContextData();

            // assert
            expect(component.instance.owner).toBeTruthy();
            expect(component.instance.agent).toBeTruthy();
            expect(component.instance.assetGroup).toBeFalsy();
        });

        it("for an asset group", () => {
            // arrange
            let component = createComponent({ assetGroup });

            // act
            component.instance.getPageContextData();

            // assert
            expect(component.instance.owner).toBeTruthy();
            expect(component.instance.agent).toBeFalsy();
            expect(component.instance.assetGroup).toBeTruthy();

        });

        it("for a variant request", () => {
            // arrange
            let component = createComponent({ variantRequest });

            // act
            component.instance.getPageContextData();

            // assert
            expect(component.instance.owner).toBeTruthy();
            expect(component.instance.agent).toBeFalsy();
            expect(component.instance.assetGroup).toBeFalsy();
        });
    });

    describe("onChanges for binding", () => {
        let spy: SpyCache<DefaultPageContextComponent>;
        let component: ComponentInstance<DefaultPageContextComponent>;

        beforeEach(() => {
            component = createComponent({ owner });
            spy = new SpyCache(component.instance);
            spy.getFor("getPageContext").and.stub();
        });

        it("will not get the page contexts for onInit changes", () => {
            // arrange / act
            component.instance.$onChanges({
                owner: {
                    previousValue: undefined,
                    currentValue: null,
                    isFirstChange: () => false
                }
            });

            // assert
            expect(spy.getFor("getPageContext")).not.toHaveBeenCalled();
        });

        it("will get the page contexts for binding changes", () => {
            // arrange / act
            component.instance.$onChanges({
                owner: {
                    previousValue: null,
                    currentValue: owner,
                    isFirstChange: () => false
                }
            });

            // assert
            expect(spy.getFor("getPageContext")).toHaveBeenCalled();
        });
    });

    function createComponent(params: ComponentParams): ComponentInstance<DefaultPageContextComponent> {
        return spec.createComponent<DefaultPageContextComponent>({
            markup: `<pcd-default-page-context
                pcd-owner-id=ownerId
                pcd-agent-id=agentId
                pcd-asset-group-id=assetGroupId
                pcd-variant-request-id=variantRequestId
                pcd-owner=owner
                pcd-agent=agent
                pcd-asset-group=assetGroup
                pcd-variant-request=variantRequest
            ></pcd-default-page-context>`,
            data: {
                ownerId: params.ownerId,
                agentId: params.agentId,
                assetGroupId: params.assetGroupId,
                variantRequestId: params.variantRequestId,
                owner: params.owner,
                agent: params.agent,
                assetGroup: params.assetGroup,
                variantRequest: params.variantRequest,
            }
        });
    }
});

