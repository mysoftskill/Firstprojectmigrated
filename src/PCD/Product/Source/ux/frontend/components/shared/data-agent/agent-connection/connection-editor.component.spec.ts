import { TestSpec, ComponentInstance, SpyCache } from "../../../../shared-tests/spec.base";

import AgentConnectionEditorComponent from "./connection-editor.component";
import ConnectionEditorPivotComponent from "./connection-editor-pivot.component";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import * as SelectList from "../../../../shared/select-list";
import { ILockdownService } from "../../../../shared/lockdown.service";

describe("Agent connection editor", () => {
    let spec: TestSpec;
    let pdmsDataServiceMock: SpyCache<Pdms.IPdmsDataService>;
    let lockdownMock: SpyCache<ILockdownService>;
    let mockConnectionEditorPivotComponent: ConnectionEditorPivotComponent;

    beforeEach(() => {
        spec = new TestSpec();
        let meeComponentRegistrySpy: SpyCache<MeePortal.OneUI.Angular.IMeeComponentRegistryService>;

        inject((
            _pdmsDataService_: Pdms.IPdmsDataService,
            _lockdownService_: ILockdownService
        ) => {
            pdmsDataServiceMock = new SpyCache(_pdmsDataService_);
            pdmsDataServiceMock.getFor("resetDataAgentConnectionDetails").and.callThrough();
            lockdownMock = new SpyCache(_lockdownService_);
        });

        meeComponentRegistrySpy = new SpyCache(spec.$meeComponentRegistry);

        mockConnectionEditorPivotComponent = new ConnectionEditorPivotComponent(null, null, null, null, spec.$meeComponentRegistry, null);
        meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(mockConnectionEditorPivotComponent);
    });

    describe("for delete agent", () => {
        it("initializes the state correctly", () => {
            // arrange/act
            let component = createComponent();

            // assert
            expect(component.instance.releaseState).toEqual("Ring1");

            expect(component.instance.connectionDetails).toBeTruthy();          //  Copied from input.
            expect(component.instance.protocolPickerModel).toBeTruthy();        //  Copied from input.

            expect(component.instance.prodReadinessState).toEqual("ProdReady");
            expect(component.instance.hasExistingProdReadyConnection).toBeTruthy();

            expect(component.instance.authTypePickerModel).toBeTruthy();
            expect(component.instance.authTypePickerModel.items.length).toBe(2);
            expect(component.instance.authTypePickerModel.selectedId).toEqual(component.instance.connectionDetails.authenticationType);
        });

        describe("when authTypeChanged() is called", () => {
            it("resets connection details", () => {
                // arrange
                let component = createComponent();
                component.instance.connectionDetails.authenticationType = Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth];
                component.instance.connectionDetails.aadAppId = "Some-app-id";

                // act
                component.instance.authTypeChanged();

                // assert
                expect(component.instance.connectionDetails.aadAppId).toBeFalsy();
                expect(component.instance.connectionDetails.msaSiteId).toBeFalsy();
            });

            it("resets errors on other connection details", () => {
                // arrange
                let connectionEditorPivotComponentSpy = new SpyCache<ConnectionEditorPivotComponent>(mockConnectionEditorPivotComponent);
                connectionEditorPivotComponentSpy.getFor("resetErrorsOnAllEditors").and.callThrough();

                let component = createComponent();
                component.instance.connectionDetails.authenticationType = Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth];
                component.instance.connectionDetails.aadAppId = "Some-app-id";

                // act
                component.instance.authTypeChanged();

                // assert
                expect(connectionEditorPivotComponentSpy.getFor("resetErrorsOnAllEditors")).toHaveBeenCalledWith("aad-app-id");
                expect(connectionEditorPivotComponentSpy.getFor("resetErrorsOnAllEditors")).toHaveBeenCalledWith("msa-site-id");
            });
        });

        describe("when applyChanges() is called", () => {
            it("applies selected protocol", () => {
                // arrange
                let component = createComponent();

                // act
                component.instance.protocolPickerModel.selectedId = Pdms.PrivacyProtocolId.CosmosDeleteSignalV2;
                component.instance.applyChanges();

                // assert
                expect(component.instance.connectionDetails.protocol).toEqual(Pdms.PrivacyProtocolId.CosmosDeleteSignalV2);
            });
        });

        describe("change of authenticationType value", () => {
            let component: ComponentInstance<AgentConnectionEditorComponent>;

            beforeEach(() => {
                // arrange
                component = createComponent();

                component.instance.connectionDetails.authenticationType = Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth];
                component.instance.connectionDetails.aadAppId = "8E29502F-4AC2-4834-AACD-D9EB55971551";
                component.instance.connectionDetails.msaSiteId = 123;
            });

            it("removes unnecessary auth properties, if AadAppBasedAuth is selected", () => {
                // act
                component.instance.authTypePickerModel.selectedId = Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth];
                component.instance.applyChanges();

                // assert
                expect(component.instance.connectionDetails.authenticationType).toBe(Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth]);
                expect(component.instance.connectionDetails.aadAppId).toBe("8E29502F-4AC2-4834-AACD-D9EB55971551");
                expect(component.instance.connectionDetails.msaSiteId).toBeFalsy();

                it("removes unnecessary auth properties, if MsaSiteBasedAuth is selected", () => {
                    // act
                    component.instance.authTypePickerModel.selectedId = Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth];
                    component.instance.applyChanges();

                    // assert
                    expect(component.instance.connectionDetails.authenticationType).toBe(Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth]);
                    expect(component.instance.connectionDetails.aadAppId).toBeFalsy();
                    expect(component.instance.connectionDetails.msaSiteId).toBe(123);
                });
            });
        });

        describe("input fields", () => {
            let component: ComponentInstance<AgentConnectionEditorComponent>;

            beforeEach(() => {
                // arrange
                component = createComponent();
            });

            it("change visibility, if CosmosDeleteSignalV2 protocol is selected", () => {
                // act
                component.instance.protocolPickerModel.selectedId = Pdms.PrivacyProtocolId.CosmosDeleteSignalV2;
                component.instance.protocolChanged();

                // assert
                expect(component.instance.isAuthTypePickerVisible()).toBe(false);
                expect(component.instance.isMsaSiteIdVisible()).toBe(false);
                expect(component.instance.isAadAppIdVisible()).toBe(false);
            });

            it("change visibility, if CommandFeedV1 protocol and AAD app-based auth are selected", () => {
                // act
                component.instance.protocolPickerModel.selectedId = Pdms.PrivacyProtocolId.CommandFeedV1;
                component.instance.protocolChanged();

                component.instance.authTypePickerModel.selectedId = Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth];

                // assert
                expect(component.instance.isAuthTypePickerVisible()).toBe(true);
                expect(component.instance.isMsaSiteIdVisible()).toBe(false);
                expect(component.instance.isAadAppIdVisible()).toBe(true);
            });

            it("change visibility, if CommandFeedV1 protocol and MSA site-based auth are selected", () => {
                // act
                component.instance.protocolPickerModel.selectedId = Pdms.PrivacyProtocolId.CommandFeedV1;
                component.instance.protocolChanged();

                component.instance.authTypePickerModel.selectedId = Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth];

                // assert
                expect(component.instance.isAuthTypePickerVisible()).toBe(true);
                expect(component.instance.isMsaSiteIdVisible()).toBe(true);
                expect(component.instance.isAadAppIdVisible()).toBe(false);
            });

            it("gets selected protocol label", () => {
                // arrange
                component.instance.protocolPickerModel.selectedId = Pdms.PrivacyProtocolId.CommandFeedV1;

                // assert
                expect(component.instance.getSelectedProtocolLabel()).toBe(component.instance.protocolPickerModel.items[1].label);  // the index matches the order of items in protocolPickerModel, as they were set up.
            });

            it("gets selected protocol label as protocol ID, if ID wasn't found in protocol picker model", () => {
                // arrange
                component.instance.protocolPickerModel.selectedId = "whatever";

                // assert
                expect(component.instance.getSelectedProtocolLabel()).toBe("whatever");
            });
        });

        describe("for Prod readiness state", () => {
            it("shows prod upgrade warning in red when changed to Prod ready", () => {
                // arrange
                let component = createComponent();
                component.instance.hasExistingProdReadyConnection = false;

                // act
                component.instance.prodReadinessChanged();
                let showProdUpgradeWarning = component.instance.showProdUpgradeWarning();
                let showWarningInRed = component.instance.showProdUpgradeWarningInRed();

                // assert
                expect(showProdUpgradeWarning).toBeTruthy();
                expect(showWarningInRed).toBeTruthy();
            });

            it("shows prod upgrade warning in black when pre-configured ProdReady state is there", () => {
                // arrange
                let component = createComponent();

                // act
                let showProdUpgradeWarning = component.instance.showProdUpgradeWarning();
                let showWarningInRed = component.instance.showProdUpgradeWarningInRed();

                // assert
                expect(showProdUpgradeWarning).toBeTruthy();
                expect(showWarningInRed).toBeFalsy();
            });

            it("disables TestingInProduction option when pre-configured ProdReady state is there", () => {
                // arrange
                let component = createComponent();

                // act
                let disableTestInProdMode = component.instance.disableTestInProdMode();

                // assert
                expect(disableTestInProdMode).toBeTruthy();
            });

            it("allows switch to Production Ready, if IcM connector ID is provided and lockdown is not active", () => {
                // arrange
                let component = createComponent();

                // act
                let disableProdReadyMode = component.instance.disableProdReadyMode();

                // assert
                expect(disableProdReadyMode).toBeFalsy();
            });

            it("doesn't show warning about switch to Production Ready being disabled, if IcM connector ID is provided and lockdown is not active", () => {
                // arrange
                let component = createComponent();

                // act
                let showWarning = component.instance.showProdUpgradeDisabledWarning();

                // assert
                expect(showWarning).toBeFalsy();
            });

            it("disables switch to Production Ready, if no IcM connector ID is provided", () => {
                // arrange
                let component = createComponent();
                component.instance.hasIcmConnectorId = false;

                // act
                let disableProdReadyMode = component.instance.disableProdReadyMode();

                // assert
                expect(disableProdReadyMode).toBeTruthy();
            });

            it("shows warning about switch to Production Ready being disabled, if IcM connector ID is provided", () => {
                // arrange
                let component = createComponent();
                component.instance.hasIcmConnectorId = false;

                // act
                let showWarning = component.instance.showProdUpgradeDisabledWarning();

                // assert
                expect(showWarning).toBeTruthy();
            });

            it("disables switch to Production Ready, if lockdown is active and initial state is Testing In Production", () => {
                // arrange
                let component = createComponent(Pdms.AgentReadinessState.TestInProd);
                lockdownMock.getFor("isActive").and.returnValue(true);

                // act
                let disableProdReadyMode = component.instance.disableProdReadyMode();

                // assert
                expect(disableProdReadyMode).toBeTruthy();
            });

            it("shows warning about switch to Production Ready being disabled, if lockdown is active and initial state is Testing In Production", () => {
                // arrange
                let component = createComponent(Pdms.AgentReadinessState.TestInProd);
                lockdownMock.getFor("isActive").and.returnValue(true);

                // act
                let showWarning = component.instance.showProdUpgradeDisabledWarning();

                // assert
                expect(showWarning).toBeTruthy();
            });

            it("allows switch to Production Ready, if lockdown is active and initial state is Production Ready", () => {
                // arrange
                let component = createComponent();
                lockdownMock.getFor("isActive").and.returnValue(true);

                // act
                let disableProdReadyMode = component.instance.disableProdReadyMode();

                // assert
                expect(disableProdReadyMode).toBeFalsy();
            });

            it("doesn't show warning about switch to Production Ready being disabled, if lockdown is active and initial state is Production Ready", () => {
                // arrange
                let component = createComponent();
                lockdownMock.getFor("isActive").and.returnValue(true);

                // act
                let showWarning = component.instance.showProdUpgradeDisabledWarning();

                // assert
                expect(showWarning).toBeFalsy();
            });
        });

        function createComponent(agentReadiness: Pdms.AgentReadinessState = Pdms.AgentReadinessState.ProdReady): ComponentInstance<AgentConnectionEditorComponent> {
            let connectionDetails: Pdms.DataAgentConnectionDetails = {
                authenticationType: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
                msaSiteId: 2,
                protocol: Pdms.PrivacyProtocolId.CommandFeedV1,
                releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                agentReadiness: Pdms.AgentReadinessState[agentReadiness]
            };
            let protocolPickerModel: SelectList.Model = {
                selectedId: connectionDetails.protocol,
                items: [{
                    id: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2,
                    label: `label - ${Pdms.PrivacyProtocolId.CosmosDeleteSignalV2}`
                }, {
                    id: Pdms.PrivacyProtocolId.CommandFeedV1,
                    label: `label - ${Pdms.PrivacyProtocolId.CommandFeedV1}`
                }]
            };

            return spec.createComponent<AgentConnectionEditorComponent>({
                markup: `<pcd-agent-connection-editor
                             connection-details=connectionDetails
                             protocol-picker-model=protocolPickerModel
                             has-icm-connector-id="true"
                             release-state="Ring1">
                         </pcd-agent-connection-editor>`,
                data: {
                    connectionDetails: connectionDetails,
                    protocolPickerModel: protocolPickerModel
                }
            });
        }
    });
});
