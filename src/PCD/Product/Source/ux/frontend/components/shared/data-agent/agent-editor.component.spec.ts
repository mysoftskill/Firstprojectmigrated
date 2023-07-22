import { ComponentInstance, TestSpec } from "../../../shared-tests/spec.base";
import { SpyCache } from "../../../shared-tests/spy-cache";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import * as Pdms from "../../../shared/pdms/pdms-types";
import AgentConnectionEditorPivotComponent from "./agent-connection/connection-editor-pivot.component";
import AgentEditorComponent from "./agent-editor.component";

describe("Agent Editor", () => {
    let spec: TestSpec;
    let $modalState: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let pcdErrorService: SpyCache<IPcdErrorService>;
    let agent: Pdms.DataAgent;
    let meeComponentRegistrySpy: SpyCache<MeePortal.OneUI.Angular.IMeeComponentRegistryService>;

    beforeEach(() => {
        agent = {
            kind: "delete-agent",
            assetGroups: null,
            id: "agent1",
            name: "sample agent name",
            description: "agent",
            ownerId: "4cb3c61d-cc1f-44be-ac24-0114de4fcf07",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            hasSharingRequests: true,
            icmConnectorId: "00000000-0000-0000-0000-000000000000",
            connectionDetails: {
                Prod: {
                    authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
                    msaSiteId: 2,
                    protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.Prod],
                    agentReadiness: Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd]
                }
            },
            operationalReadiness: null,
            deploymentLocation: Pdms.PrivacyCloudInstanceId.Public,
            supportedClouds: [
                Pdms.PrivacyCloudInstanceId.Public,
                Pdms.PrivacyCloudInstanceId.Mooncake
            ],
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
    };

        spec = new TestSpec();

        meeComponentRegistrySpy = new SpyCache(spec.$meeComponentRegistry);

        inject((
            _$meeModal_: MeePortal.OneUI.Angular.IModalStateService,
            _pcdErrorService_: IPcdErrorService
        ) => {
            $modalState = new SpyCache(_$meeModal_);
            pcdErrorService = new SpyCache(_pcdErrorService_);
        });
    });

    describe("warning prompt for NGP data processing", () => {
        beforeEach(() => {
            pcdErrorService.getFor("resetErrorsForCategory").and.stub();
        });

        it("is displayed when Prod connection detail is updated to ProdReady", () => {
            //arrange
            let component = createComponent();
            $modalState.getFor("show").and.stub();
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("updateDataAgent");
            component.instance.agent.connectionDetails["Prod"].agentReadiness = Pdms.AgentReadinessState[Pdms.AgentReadinessState.ProdReady];

            //act
            component.instance.saveClicked();

            //assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("updateDataAgent")).toHaveBeenCalled();
            spec.runDigestCycle();
            expect($modalState.getFor("show")).toHaveBeenCalledWith("#modal-dialog", ".ngp-warning-prompt", jasmine.any(Object));
        });

        it("is not displayed when Prod connection detail was already set to ProdReady", () => {
            //arrange
            agent.connectionDetails["Prod"].agentReadiness = Pdms.AgentReadinessState[Pdms.AgentReadinessState.ProdReady];
            let component = createComponent();

            $modalState.getFor("show").and.stub();
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("updateDataAgent");

            //act
            component.instance.saveClicked();

            //assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("updateDataAgent")).toHaveBeenCalled();
            spec.runDigestCycle();
            expect($modalState.getFor("show")).not.toHaveBeenCalledWith("#modal-dialog", ".ngp-warning-prompt", jasmine.any(Object));
        });
    });

    it("isCosmosAgent() is true when a connection detail's protocol is set to Cosmos", () => {
        //arrange
        pcdErrorService.getFor("resetErrorsForCategory").and.stub();
        agent.connectionDetails["Prod"].protocol = Pdms.PrivacyProtocolId.CosmosDeleteSignalV2;

        let component = createComponent();

        //assert
        expect(component.instance.hideSupportedCloudsInput()).toBeTruthy();
        expect(component.instance.getSupportedCloudsAsLabel()).toEqual(Pdms.PrivacyCloudInstanceId.All);
    });

    describe("supported clouds input", () => {

        beforeEach(() => {
            agent.deploymentLocation = "";
            agent.supportedClouds = [];
            pcdErrorService.getFor("resetErrorsForCategory").and.stub();
        });

        it("is visible when deployment location is set to Public", () => {
            //arrange
            let component = createComponent();

            //act
            component.instance.nonCosmosDeploymentLocationPickerModel.selectedId = Pdms.PrivacyCloudInstanceId.Public;

            //assert
            expect(component.instance.hideSupportedCloudsInput()).toBeFalsy();
        });

        it("is hidden when deployment location is set to a cloud instance other then Public and displays the correct supported cloud", () => {
            //arrange
            let component = createComponent();

            //act
            component.instance.nonCosmosDeploymentLocationPickerModel.selectedId = Pdms.PrivacyCloudInstanceId.Fairfax;

            //assert
            expect(component.instance.hideSupportedCloudsInput()).toBeTruthy();
            expect(component.instance.getSupportedCloudsAsLabel()).toEqual(Pdms.PrivacyCloudInstanceId.Fairfax);
        });

        it("is hidden when connection details is set to a cosmos protocol", () => {
            //arrange
            agent.connectionDetails = {
                PreProd: {
                    msaSiteId: null,
                    protocol: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2,
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    agentReadiness: Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd]
                }
            };

            // act
            let component = createComponent();

            //assert
            expect(component.instance.hideSupportedCloudsInput()).toBeTruthy();
            expect(component.instance.getSupportedCloudsAsLabel()).toEqual(Pdms.PrivacyCloudInstanceId.All);
        });
    });

    function createComponent(): ComponentInstance<AgentEditorComponent> {
        let owner: Pdms.DataOwner = {
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

        return spec.createComponent<AgentEditorComponent>({
            markup: `<pcd-agent-editor 
                        owner=owner
                        agent=agent
                        on-saved=onSaved
                        modal-return-location=modalReturnLocation></pcd-agent-editor>`,
            data: {
                owner: owner,
                agent: agent,
                onSaved: () => { },
                modalReturnLocation: "^"
            }
        });
    }

    function createConnectionEditorPivotComponent(customConnectionDetailsGroup?: Pdms.DataAgentConnectionDetailsGroup):
        ComponentInstance<AgentConnectionEditorPivotComponent> {
        let connectionDetailsGroup: Pdms.DataAgentConnectionDetailsGroup = customConnectionDetailsGroup || {
            PreProd: {
                authenticationType: "MsaSiteBasedAuth",
                msaSiteId: 1,
                protocol: "CommandFeedV1",
                releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd]
            },
            Ring1: {
                authenticationType: "MsaSiteBasedAuth",
                msaSiteId: 2,
                protocol: "CommandFeedV1",
                releaseState: Pdms.ReleaseState[Pdms.ReleaseState.Ring1]
            }
        };

        return spec.createComponent<AgentConnectionEditorPivotComponent>({
            markup: `<pcd-agent-connection-editor-pivot
                            connection-details-group=connectionDetailsGroup
                            agent-kind="delete-agent">
                    </pcd-agent-connection-editor-pivot>`,
            data: {
                connectionDetailsGroup: connectionDetailsGroup
            }
        });
    }
});
