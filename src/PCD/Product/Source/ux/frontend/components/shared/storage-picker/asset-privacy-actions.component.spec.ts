import { TestSpec, ComponentInstance } from "../../../shared-tests/spec.base";
import { AssetPrivacyActionsComponent } from "./asset-privacy-actions.component";
import { DataOwnershipIndicator } from "./asset-owner-indicator.component";

import * as Pdms from "../../../shared/pdms/pdms-types";

type ComponentOptions = {
    isDataOwner?: DataOwnershipIndicator;
    protocol?: string;
    assetGroup?: Pdms.AssetGroup;
    dataAgentId?: string;
};

describe("AssetPrivacyActionsComponent", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();
    });

    it("sets the properties correctly onInit", () => {
        // arrange / act
        let component = createComponent();

        // assert
        expect(component.instance.privacyActionState.deleteAction).toBeFalsy();
        expect(component.instance.privacyActionState.exportAction).toBeFalsy();
        expect(component.instance.enabledActions.deleteAction).toBeTruthy();
        expect(component.instance.enabledActions.exportAction).toBeFalsy();
    });

    describe("$onChanges", () => {
        it("changes to data owner invalidates export action", () => {
            let component = createComponent({ isDataOwner: "yes" });
            component.instance.privacyActionState.exportAction = true;

            // Simulate onChanges
            component.instance.isDataOwner = "no";
            component.instance.$onChanges(<ng.IOnChangesObject>{
                isDataOwner: <ng.IChangesObject<DataOwnershipIndicator>>{
                    previousValue: "yes",
                    currentValue: "no",
                    isFirstChange: () => false
                }
            });

            expect(component.instance.enabledActions.exportAction).toBeFalsy();
            expect(component.instance.privacyActionState.exportAction).toBeFalsy();
        });

        it("sets privacy actions to false if there is no asset group", () => {
            let component = createComponent({ isDataOwner: "yes" });

            expect(component.instance.privacyActionState.deleteAction).toBeFalsy();
            expect(component.instance.privacyActionState.exportAction).toBeFalsy();
        });

        it("sets existing privacy actions", () => {
            let sampleAssetGroup: Pdms.AssetGroup = {
                id: "GUID-545",
                deleteAgentId: "anyDataAgentId",
                exportAgentId: "",
                ownerId: "GUID-888",
                qualifier: {
                    props: {
                        "AssetType": "File",
                        "ServerPath": "ExampleServer",
                        "FileName": "File-y.txt"
                    }
                }
            };
            let component = createComponent({
                isDataOwner: "yes",
                assetGroup: sampleAssetGroup,
                dataAgentId: "SpecificId"
            });

            expect(component.instance.privacyActionState.deleteAction).toBeTruthy();
        });
    });

    describe("when team owns data", () => {
        describe("with a fully functioning protocol", () => {
            it("enables all actions", () => {
                // arrange / act
                let component = createComponent({ isDataOwner: "yes", protocol: "CosmosDeleteSignalV2" });

                // assert
                expect(areAllActionsEnabled(component.instance.enabledActions)).toBeFalsy();
            });
        });
    });

    describe("when team does not own data", () => {
        describe("with a fully functioning protocol", () => {
            it("enables only delete", () => {
                // arrange / act
                let component = createComponent({ isDataOwner: "no", protocol: "CosmosDeleteSignalV2" });

                // assert
                expect(getEnabledActions(component.instance.enabledActions)).toEqual(["deleteAction"]);
            });
        });
    });

    describe("asset linked to a different agent than the contextual agent", () => {
        let sampleAssetGroup: Pdms.AssetGroup;

        beforeEach(() => {
            sampleAssetGroup = {
                id: "GUID-545",
                deleteAgentId: "",
                exportAgentId: "",
                ownerId: "GUID-888",
                qualifier: {
                    "props": {
                        "AssetType": "File",
                        "ServerPath": "ExampleServer",
                        "FileName": "File-y.txt"
                    }
                }
            };
        });

        it("disables the action", () => {
            sampleAssetGroup.deleteAgentId = "Other agent ID";
            let component = createComponent({
                isDataOwner: "yes",
                assetGroup: sampleAssetGroup,
                dataAgentId: "SpecificId"
            });

            expect(component.instance.enabledActions.deleteAction).toBeFalsy();
        });

        it("disabled action is still selected", () => {
            sampleAssetGroup.deleteAgentId = "Other agent ID";
            let component = createComponent({
                isDataOwner: "yes",
                assetGroup: sampleAssetGroup,
                dataAgentId: "SpecificId"
            });

            expect(component.instance.privacyActionState.deleteAction).toBeTruthy();
        });

        it("disables other actions by existing rules (export is disabled if you're not an owner)", () => {
            sampleAssetGroup.deleteAgentId = "SpecificId";
            sampleAssetGroup.exportAgentId = "";
            let component = createComponent({
                isDataOwner: "no",
                assetGroup: sampleAssetGroup,
                dataAgentId: "SpecificId"
            });

            expect(component.instance.enabledActions.exportAction).toBeFalsy();
        });

        it("enables the action when it can be linked", () => {
            let component = createComponent({
                isDataOwner: "yes",
                protocol: "any",
                assetGroup: sampleAssetGroup,
                dataAgentId: "SpecificId"
            });

            expect(component.instance.enabledActions.deleteAction).toBeTruthy();
            expect(component.instance.enabledActions.exportAction).toBeTruthy();
        });

        it("enables the action when it matches the data agent id", () => {
            sampleAssetGroup.deleteAgentId = "SpecificId";
            sampleAssetGroup.exportAgentId = "SpecificId";
            let component = createComponent({
                isDataOwner: "yes",
                protocol: "any",
                assetGroup: sampleAssetGroup, dataAgentId: "SpecificId"
            });

            expect(component.instance.enabledActions.deleteAction).toBeTruthy();
            expect(component.instance.enabledActions.exportAction).toBeTruthy();
        });
    });

    function getEnabledActions(actions: Pdms.PrivacyActionsState): string[] {
        return _.filter(Object.keys(actions), (action: string) => actions[action]);
    }

    function areAllActionsEnabled(actions: Pdms.PrivacyActionsState): boolean {
        let states = _.values(actions);
        return _.every(states, (s: boolean) => s === false);
    }

    function areAllActionsDisabled(actions: Pdms.PrivacyActionsState): boolean {
        let states = _.values(actions);
        return _.every(states, (s: boolean) => s === true);
    }

    function createComponent(options: ComponentOptions = { isDataOwner: "no", protocol: "any" }): ComponentInstance<AssetPrivacyActionsComponent> {
        return spec.createComponent<AssetPrivacyActionsComponent>({
            markup: `<pcd-asset-privacy-actions ng-model="$ctrl.model.privacyActionState"
                                                pcd-is-data-owner="isDataOwner"
                                                pcd-agent-protocol="{{protocol}}"
                                                pcd-asset-group=assetGroup
                                                pcd-data-agent-id=dataAgentId></pcd-asset-privacy-actions>`,
            data: {
                isDataOwner: options.isDataOwner,
                protocol: options.protocol,
                assetGroup: options.assetGroup,
                dataAgentId: options.dataAgentId
            }
        });
    }
});
