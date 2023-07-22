import { ComponentInstance, SpyCache, TestSpec } from "../../../../shared-tests/spec.base";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import { SelectListItem } from "../../../../shared/select-list";
import AgentConnectionEditorPivotComponent from "./connection-editor-pivot.component";
import AgentConnectionEditorComponent from "./connection-editor.component";

describe("Agent connection editor pivot", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();
    });

    describe("for delete agent", () => {
        it("initializes the state correctly", () => {
            // act
            let component = createComponent();

            // assert
            expect(component.instance.agentKind).toEqual("delete-agent");
            expect(component.instance.allowedReleaseStates).toEqual(["PreProd", "Ring1", "Prod"]);

            expect(component.instance.connectionDetailsGroup).toBeTruthy();
            expect(component.instance.connectionDetailsGroup.PreProd).toBeTruthy();
            expect(component.instance.connectionDetailsGroup.PreProd.authenticationType).toEqual("MsaSiteBasedAuth");
            expect(component.instance.connectionDetailsGroup.PreProd.protocol).toEqual("CommandFeedV1");
            expect(component.instance.connectionDetailsGroup.PreProd.msaSiteId).toEqual(1);

            expect(component.instance.connectionDetailsGroup.Ring1).toBeTruthy();
            expect(component.instance.connectionDetailsGroup.Ring1.authenticationType).toEqual("MsaSiteBasedAuth");
            expect(component.instance.connectionDetailsGroup.Ring1.protocol).toEqual("CommandFeedV1");
            expect(component.instance.connectionDetailsGroup.Ring1.msaSiteId).toEqual(2);

            expect(component.instance.displayedReleaseStates).toEqual(["PreProd", "Ring1"]);
        });

        it("adds connection", () => {
            // arrange
            let component = createComponent();
            expect(Object.keys(component.instance.connectionDetailsGroup).length).toEqual(2);
            component.instance.connectionDetailsGroup.PreProd.agentReadiness = component.instance.connectionDetailsGroup.Ring1.agentReadiness = Pdms.AgentReadinessState[Pdms.AgentReadinessState.ProdReady];

            // act
            component.instance.addClicked();

            // assert
            expect(component.instance.allowedReleaseStates).toEqual(["PreProd", "Ring1", "Prod"]);

            expect(Object.keys(component.instance.connectionDetailsGroup).length).toEqual(3);
            expect(component.instance.connectionDetailsGroup.Prod).toBeTruthy();
            expect(component.instance.connectionDetailsGroup.Prod.authenticationType).toEqual("MsaSiteBasedAuth");
            expect(component.instance.connectionDetailsGroup.Prod.protocol).toEqual("CommandFeedV1");
            expect(component.instance.connectionDetailsGroup.Prod.aadAppId).toEqual(undefined);
            expect(component.instance.connectionDetailsGroup.Prod.msaSiteId).toEqual(undefined);
            expect(component.instance.connectionDetailsGroup.Prod.agentReadiness).toEqual(Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd]);
        });

        it("removes connection", () => {
            // arrange
            let component = createComponent();
            expect(Object.keys(component.instance.connectionDetailsGroup).length).toEqual(2);

            // act
            component.instance.removeClicked("Ring1");

            // assert
            expect(component.instance.allowedReleaseStates).toEqual(["PreProd", "Ring1", "Prod"]);

            expect(Object.keys(component.instance.connectionDetailsGroup).length).toEqual(1);
            expect(component.instance.connectionDetailsGroup.PreProd).toBeTruthy();
            expect(component.instance.connectionDetailsGroup.PreProd.authenticationType).toEqual("MsaSiteBasedAuth");
            expect(component.instance.connectionDetailsGroup.PreProd.protocol).toEqual("CommandFeedV1");
            expect(component.instance.connectionDetailsGroup.PreProd.msaSiteId).toEqual(1);
        });

        it("has connection details of non-Prod states as editable when there is a Prod connection", () => {
            // arrange
            let component = createComponent();
            component.instance.hasExistingProdConnection = true;

            // act
            let areConnectionDetailsReadOnly = component.instance.areConnectionDetailsReadOnly();

            // assert
            expect(areConnectionDetailsReadOnly).toBeFalsy();
        });

        it("has connection details of Prod state as disabled when there is a Prod connection and is active", () => {
            // arrange
            let component = createComponent();
            component.instance.hasExistingProdConnection = true;
            component.instance.setActiveState("Prod");

            // act
            let areConnectionDetailsReadOnly = component.instance.areConnectionDetailsReadOnly();

            // assert
            expect(areConnectionDetailsReadOnly).toBeTruthy();
        });

        it("shows Prod immutable warning when adding a Prod connection", () => {
            // arrange
            let component = createComponent();
            component.instance.displayedReleaseStates.push("Prod");

            // act
            let showProdImmutableWarning = component.instance.showProdImmutableWarning();

            // assert
            expect(showProdImmutableWarning).toBeTruthy();
        });

        it("shows Prod immutable message when Prod state is pre-configured and is active", () => {
            // arrange
            let component = createComponent();
            component.instance.displayedReleaseStates.push("Prod");
            component.instance.hasExistingProdConnection = true;
            component.instance.setActiveState("Prod");

            // act
            let showProdImmutableMessage = component.instance.showProdImmutableMessage();

            // assert
            expect(showProdImmutableMessage).toBeTruthy();
        });

        it("does not show Prod immutable message when Prod state is pre-configured and is NOT active", () => {
            // arrange
            let component = createComponent();
            component.instance.displayedReleaseStates.push("Prod");
            component.instance.hasExistingProdConnection = true;
            component.instance.setActiveState("PreProd");

            // act
            let showProdImmutableMessage = component.instance.showProdImmutableMessage();

            // assert
            expect(showProdImmutableMessage).toBeFalsy();
        });

        it("hides protocol picker when Prod is pre-configured", () => {
            // arrange
            let component = createComponent();
            component.instance.hasExistingProdConnection = true;

            // act
            let showProtocolPicker = component.instance.showProtocolPicker();

            // assert
            expect(showProtocolPicker).toBeFalsy();
        });

        it("shows add button when Prod state is pre-configured", () => {
            // arrange
            let component = createComponent();
            component.instance.hasExistingProdConnection = true;

            // act
            let showAddbutton = component.instance.showAddButton();

            // assert
            expect(showAddbutton).toBeTruthy();
        });

        it("shows removal for other states when Prod state is pre-configured", () => {
            // arrange
            let component = createComponent();
            component.instance.hasExistingProdConnection = true;

            // act
            let showRemovebutton = component.instance.showRemoveButton("PreProd");

            // assert
            expect(showRemovebutton).toBeTruthy();
        });

        it("hides removal for Prod state when it is pre-configured", () => {
            // arrange
            let component = createComponent();
            component.instance.hasExistingProdConnection = true;

            // act
            let showRemovebutton = component.instance.showRemoveButton("Prod");

            // assert
            expect(showRemovebutton).toBeFalsy();
        });

        it("hides removal button for Prod state when it is not pre-configured AND the only state in pivot", () => {
            // arrange
            let component = createComponent();
            component.instance.removeClicked("Ring1");
            component.instance.hasExistingProdConnection = false;

            // act
            let showRemovebutton = component.instance.showRemoveButton("Prod");

            // assert
            expect(showRemovebutton).toBeFalsy();
        });

        it("shows removal for Prod state when it is not pre-configured AND there are more than one states in pivot", () => {
            // arrange
            let component = createComponent();
            component.instance.hasExistingProdConnection = false;

            // act
            let showRemovebutton = component.instance.showRemoveButton("Prod");

            // assert
            expect(showRemovebutton).toBeTruthy();
        });

        it("updates release state picker correctly on connection addition", () => {
            // arrange
            let component = createComponent();
            expect(Object.keys(component.instance.releaseStatePickerModel.items).length).toEqual(1);

            // act
            component.instance.addClicked();

            // assert
            expect(Object.keys(component.instance.releaseStatePickerModel.items).length).toEqual(0);
        });

        it("updates release state picker correctly on connection removal", () => {
            // arrange
            let component = createComponent();
            expect(Object.keys(component.instance.releaseStatePickerModel.items).length).toEqual(1);

            // act
            component.instance.removeClicked("Ring1");

            // assert
            expect(Object.keys(component.instance.releaseStatePickerModel.items).length).toEqual(2);
            expect(component.instance.releaseStatePickerModel.selectedId).toEqual("Ring1");
            expect(component.instance.releaseStatePickerModel.items[0].label).toEqual("Ring1");
            expect(component.instance.releaseStatePickerModel.items[1].label).toEqual("Prod");
        });
    });

    describe("error handling", () => {
        let mockPreProd: AgentConnectionEditorComponent;
        let mockProd: AgentConnectionEditorComponent;
        let preProdSpy: SpyCache<AgentConnectionEditorComponent>;
        let prodSpy: SpyCache<AgentConnectionEditorComponent>;
        let connectionDetailsGroup: Pdms.DataAgentConnectionDetailsGroup;
        let meeComponentRegistrySpy: SpyCache<MeePortal.OneUI.Angular.IMeeComponentRegistryService>;

        beforeEach(() => {
            let meeUtil: MeePortal.OneUI.Angular.IMeeUtil;

            inject((_$meeUtil_: MeePortal.OneUI.Angular.IMeeUtil) => {
                meeUtil = _$meeUtil_;
            });

            meeComponentRegistrySpy = new SpyCache(spec.$meeComponentRegistry);
            mockPreProd = new AgentConnectionEditorComponent(null, null, null, spec.$meeComponentRegistry, meeUtil);
            mockProd = new AgentConnectionEditorComponent(null, null, null, spec.$meeComponentRegistry, meeUtil);

            // These configurations must be here to ensure the connection editor identifies the correct
            // connection editor.
            mockPreProd.releaseState = Pdms.ReleaseState[Pdms.ReleaseState.PreProd];
            mockProd.releaseState = Pdms.ReleaseState[Pdms.ReleaseState.Prod];

            preProdSpy = new SpyCache<AgentConnectionEditorComponent>(mockPreProd);
            prodSpy = new SpyCache<AgentConnectionEditorComponent>(mockProd);
            connectionDetailsGroup = {
                PreProd: {
                    authenticationType: "MsaSiteBasedAuth",
                    msaSiteId: null,
                    protocol: "",
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    agentReadiness: Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd]
                },
                Prod: {
                    authenticationType: "",
                    msaSiteId: null,
                    protocol: "",
                    releaseState: Pdms.ReleaseState[Pdms.ReleaseState.Prod],
                    agentReadiness: Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd]
                }
            };
        });

        it("resets errors across all connection editors", () => {
            // arrange
            preProdSpy.getFor("resetError").and.stub();
            prodSpy.getFor("resetError").and.stub();
            meeComponentRegistrySpy.getFor("getInstancesByClass").and.returnValue([mockPreProd, mockProd]);

            let component: ComponentInstance<AgentConnectionEditorPivotComponent> = createComponent(connectionDetailsGroup);

            // act
            component.instance.resetErrorsOnAllEditors("any-error-id");

            // assert
            expect(preProdSpy.getFor("resetError")).toHaveBeenCalledWith("any-error-id");
            expect(prodSpy.getFor("resetError")).toHaveBeenCalledWith("any-error-id");
        });

        it("has no duplicate errors with only a PreProd connection", () => {
            // arrange
            delete connectionDetailsGroup["Prod"];

            preProdSpy.getFor("hasErrors").and.returnValue(false);
            meeComponentRegistrySpy.getFor("getInstancesByClass").and.returnValue([mockPreProd]);

            let component: ComponentInstance<AgentConnectionEditorPivotComponent> = createComponent(connectionDetailsGroup);

            // act
            let hasErrors = component.instance.hasErrors();

            // assert
            expect(hasErrors).toBe(false);
            expect(preProdSpy.getFor("setDuplicateAadAppIdError")).not.toHaveBeenCalled();
        });

        it("has no duplicate errors with only a Prod connection", () => {
            // arrange
            delete connectionDetailsGroup["PreProd"];

            prodSpy.getFor("hasErrors").and.returnValue(false);
            meeComponentRegistrySpy.getFor("getInstancesByClass").and.returnValue([mockProd]);

            let component: ComponentInstance<AgentConnectionEditorPivotComponent> = createComponent(connectionDetailsGroup);

            // act
            let hasErrors = component.instance.hasErrors();

            // assert
            expect(hasErrors).toBe(false);
            expect(prodSpy.getFor("setDuplicateAadAppIdError")).not.toHaveBeenCalled();
        });

        it("sets errors on PreProd and Prod for duplicate aad app id", () => {
            // arrange
            preProdSpy.getFor("hasErrors").and.returnValue(false);
            prodSpy.getFor("hasErrors").and.returnValue(false);
            preProdSpy.getFor("setDuplicateAadAppIdError").and.stub();
            prodSpy.getFor("setDuplicateAadAppIdError").and.stub();

            meeComponentRegistrySpy.getFor("getInstancesByClass").and.returnValue([mockPreProd, mockProd]);

            connectionDetailsGroup["PreProd"].aadAppId = "any-dup-app-id";
            connectionDetailsGroup["Prod"].aadAppId = "any-dup-app-id";

            let component: ComponentInstance<AgentConnectionEditorPivotComponent> = createComponent(connectionDetailsGroup);

            // act
            let hasErrors = component.instance.hasErrors();

            // assert
            expect(hasErrors).toBe(true);
            expect(preProdSpy.getFor("setDuplicateAadAppIdError")).toHaveBeenCalled();
            expect(prodSpy.getFor("setDuplicateAadAppIdError")).toHaveBeenCalled();
        });

        it("sets errors on PreProd and Prod for duplicate msa site id", () => {
            // arrange
            preProdSpy.getFor("hasErrors").and.returnValue(false);
            prodSpy.getFor("hasErrors").and.returnValue(false);
            preProdSpy.getFor("setDuplicateMsaSiteIdError").and.stub();
            prodSpy.getFor("setDuplicateMsaSiteIdError").and.stub();

            meeComponentRegistrySpy.getFor("getInstancesByClass").and.returnValue([mockPreProd, mockProd]);

            connectionDetailsGroup["PreProd"].msaSiteId = 555;
            connectionDetailsGroup["Prod"].msaSiteId = 555;

            let component: ComponentInstance<AgentConnectionEditorPivotComponent> = createComponent(connectionDetailsGroup);

            // act
            let hasErrors = component.instance.hasErrors();

            // assert
            expect(hasErrors).toBe(true);
            expect(preProdSpy.getFor("setDuplicateMsaSiteIdError")).toHaveBeenCalled();
            expect(prodSpy.getFor("setDuplicateMsaSiteIdError")).toHaveBeenCalled();
        });
    });

    function createComponent(customConnectionDetailsGroup?: Pdms.DataAgentConnectionDetailsGroup): ComponentInstance<AgentConnectionEditorPivotComponent> {
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
