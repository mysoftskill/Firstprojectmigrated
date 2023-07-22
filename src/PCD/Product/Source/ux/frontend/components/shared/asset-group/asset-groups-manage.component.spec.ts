import { ComponentInstance, TestSpec } from "../../../shared-tests/spec.base";
import { SpyCache } from "../../../shared-tests/spy-cache";
import { ActionVerb } from "../../../shared/pdms-agent-relationship-types";
import * as Pdms from "../../../shared/pdms/pdms-types";
import { AssetGroupExtended, AssetGroupsManageComponent, DataAgentLinkingContext, TransferAssetGroupContext } from "./asset-groups-manage.component";

describe("AssetGroupsManage", () => {
    let spec: TestSpec;
    let assetGroups: AssetGroupExtended[];
    let $modalState: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let dataAgent: Pdms.DataAgent;

    beforeEach(() => {
        spec = new TestSpec();

        inject(((
            _$meeModal_: MeePortal.OneUI.Angular.IModalStateService
        ) => {
            $modalState = new SpyCache(_$meeModal_);
        }));

        spec.dataServiceMocks.groundControlDataService.mockAsyncResultOf("isUserInFlight", /* value: */ true);

        assetGroups = [{
            id: "DataAsset1ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            },
            ownerId: "1",
            checked: false
        },
        {
            id: "DataAsset2ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset2Type",
                    propName: "propValue"
                }
            },
            ownerId: "1",
            checked: false
        }];

        dataAgent = {
            kind: "delete-agent",
            assetGroups: null,
            id: "agent1",
            name: "sample agent name",
            description: "agent",
            ownerId: "4cb3c61d-cc1f-44be-ac24-0114de4fcf07",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            hasSharingRequests: true,
            connectionDetails: null,
            operationalReadiness: null,
            deploymentLocation: null,
            supportedClouds: null,
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
    };
    });

    it("sets the properties correctly onInit", () => {
        let component = createComponent({ assetGroups });
        component.scope.$digest();

        expect(component.instance.assetGroups).toEqual(assetGroups);
    });

    it("any checked works", () => {
        let component = createComponent({ assetGroups });
        component.scope.$digest();

        expect(component.instance.anyChecked()).toEqual(false);

        assetGroups[0].checked = true;
        expect(component.instance.anyChecked()).toEqual(true);
    });

    it("link button enabled for owned assets only", () => {
        let component = createComponent({ assetGroups });

        assetGroups[0].checked = true;
        component.instance.ownerId = "1";
        expect(component.instance.linkingEnabled()).toEqual(true);

        component.instance.ownerId = "2";
        expect(component.instance.linkingEnabled()).toEqual(false);
    });

    it("when agentId is in context, link button navigates to modal state link-data-agent", () => {
        //arrange
        let component = createComponent({ assetGroups, dataAgent });
        assetGroups[1].checked = true;
        $modalState.getFor("show").and.stub();

        //act
        component.instance.linkDataAgent();

        //assert
        expect($modalState.getFor("show")).toHaveBeenCalled();
        let args = $modalState.getFor("show").calls.argsFor(0);
        let modalData = <DataAgentLinkingContext> args[2].data;

        expect(args[1]).toEqual(".link-data-agent");
        expect(modalData.assetGroups.length).toEqual(1);
        expect(modalData.assetGroups[0]).toEqual(assetGroups[1]);
        expect(modalData.verb).toEqual(ActionVerb.set);
    });

    it("when agentId is not in context, link button navigates to modal state select-agent", () => {
        //arrange
        let component = createComponent({ assetGroups });
        assetGroups[1].checked = true;
        $modalState.getFor("show").and.stub();

        //act
        component.instance.linkDataAgent();

        //assert
        expect($modalState.getFor("show")).toHaveBeenCalled();
        let args = $modalState.getFor("show").calls.argsFor(0);
        let modalData = <DataAgentLinkingContext> args[2].data;

        expect(args[1]).toEqual(".select-agent");
        expect(modalData.assetGroups.length).toEqual(1);
        expect(modalData.assetGroups[0]).toEqual(assetGroups[1]);
        expect(modalData.verb).toEqual(ActionVerb.set);
    });

    it("when agentId is not in context, unlink button opens modal with data correctly populated", () => {
        //arrange
        let component = createComponent({ assetGroups });
        assetGroups[1].checked = true;
        $modalState.getFor("show").and.stub();

        //act
        component.instance.unlinkDataAgent();

        //assert
        expect($modalState.getFor("show")).toHaveBeenCalled();
        let args = $modalState.getFor("show").calls.argsFor(0);
        let modalData = <DataAgentLinkingContext> args[2].data;

        expect(args[1]).toEqual(".link-data-agent");
        expect(modalData.assetGroups.length).toEqual(1);
        expect(modalData.assetGroups[0]).toEqual(assetGroups[1]);
        expect(modalData.verb).toEqual(ActionVerb.clear);
        expect(modalData.agentId).toBeFalsy();
    });

    it("when agentId is in context, unlink button opens modal with data correctly context ", () => {
        //arrange
        let component = createComponent({ assetGroups, dataAgent });
        assetGroups[1].checked = true;

        $modalState.getFor("show").and.stub();

        //act
        component.instance.unlinkDataAgent();

        //assert
        expect($modalState.getFor("show")).toHaveBeenCalled();
        let args = $modalState.getFor("show").calls.argsFor(0);
        let modalData = <DataAgentLinkingContext> args[2].data;

        expect(args[1]).toEqual(".link-data-agent");
        expect(modalData.assetGroups.length).toEqual(1);
        expect(modalData.assetGroups[0]).toEqual(assetGroups[1]);
        expect(modalData.verb).toEqual(ActionVerb.clear);
        expect(modalData.agentId).toEqual(dataAgent.id);
        expect(modalData.agentName).toEqual(dataAgent.name);
    });

    it("transfer button navigates to modal state select-transfer-owner", () => {
        //arrange
        let component = createComponent({ assetGroups, dataAgent });
        assetGroups[1].checked = true;
        $modalState.getFor("show").and.stub();

        //act
        component.instance.transferAssetGroups();

        //assert
        expect($modalState.getFor("show")).toHaveBeenCalled();
        let args = $modalState.getFor("show").calls.argsFor(0);
        let modalData = <TransferAssetGroupContext> args[2].data;

        expect(args[1]).toEqual(".select-transfer-owner");
        expect(modalData.sourceOwnerId).toEqual(component.instance.ownerId);
        expect(modalData.assetGroups.length).toEqual(1);
        expect(modalData.assetGroups[0].id).toEqual(assetGroups[1].id);
        expect(modalData.requestState).toEqual(Pdms.TransferRequestState.None);
    });

    function createComponent(data: any): ComponentInstance<AssetGroupsManageComponent> {
        return spec.createComponent<AssetGroupsManageComponent>({
            markup: `<pcd-asset-groups-manage pcd-asset-groups=assetGroups pcd-data-agent=dataAgent></pcd-asset-groups-manage>`,
            data
        });
    }
});
