import { TestSpec, SpyCache, ComponentInstance } from "../../../../shared-tests/spec.base";
import * as Pdms from "../../../../shared/pdms/pdms-types";

import AssetTransferRequestsComponent from "./asset-transfer-requests.component";

describe("AssetTransferRequestsComponent", () => {
    let spec: TestSpec;

    let $modalState: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let $stateParams: ng.ui.IStateParamsService;

    let assetGroup: Pdms.AssetGroup = {
        id: "DataAsset1ID",
        hasPendingVariantRequests: false,
        hasPendingTransferRequest: true,
        qualifier: {
            props: {
                AssetType: "DataAsset1Type",
                propName: "propValue"
            }
        }
    };

    let requests: Pdms.TransferRequest[] = [{
        id: "transferrequestid",
        sourceOwnerId: "9c5f8fe0-619a-434b-806f-46276ae13de1",
        sourceOwnerName: "Data Owner",
        targetOwnerId: "9c5f8fe0-619a-434b-806f-423gf7789",
        requestState: Pdms.TransferRequestState.None,
        assetGroups: [{
            id: "DataAsset1ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            },
            ownerId: "1",
        }]
    }];

    let owner: Pdms.DataOwner = {
        id: "9c5f8fe0-619a-434b-806f-423gf7789",
        name: "TestOwner",
        description: "BlahblahblahBlahblahblahBlahblahblahblah",
        alertContacts: [],
        announcementContacts: [],
        sharingRequestContacts: [],
        assetGroups: null,
        dataAgents: null,
        writeSecurityGroups: ["22eae02b-16cf-4fa1-b496-08eef042938742"],
        tagSecurityGroups: [],
        serviceTree: null,
        hasPendingTransferRequests: true
    };

    beforeEach(() => {
        let $q: ng.IQService;

        spec = new TestSpec();
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getTransferRequestsByTargetOwnerId", requests);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getAssetGroupById", assetGroup);
        spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", null);
        spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getSecurityGroupById", null);

        inject((
            _$stateParams_: ng.ui.IStateParamsService,
            _$meeModal_: MeePortal.OneUI.Angular.IModalStateService,
            _$q_: ng.IQService
        ) => {
            $stateParams = _$stateParams_;
            $modalState = new SpyCache(_$meeModal_);
            $q = _$q_;
        });

        $modalState.getFor("show").and.stub();
        $stateParams.ownerId = "1234567890";
    });

    it("hasTransferRequests returns true when there are pending transfer requests", () => {
        // arrange
        let component = createComponent();

        // act
        let result = component.instance.hasTransferRequests();

        // assert
        expect(result).toBeTruthy();
    });

    describe("for approve/deny modals", () => {
        it("calls the approve-transfer-request modal with the required parameters to approve a Transfer Request", () => {
            // arrange
            let component = createComponent();

            // act
            component.instance.requestContainers[0].isChecked = true;
            component.instance.approveAssetTransferRequests();

            // assert
            // TODO: Expand the data values to be more specific.
            expect($modalState.getFor("show")).toHaveBeenCalledWith(
                "#modal-dialog",
                ".approve",
                {
                    data: {
                        requests: jasmine.any(Object),
                        returnLocation: jasmine.any(String),
                        onConfirm: jasmine.any(Function)
                    }
                }
            );
        });

        it("calls the deny-transfer-request modal with the required parameters to deny a Transfer Request", () => {
            // arrange
            let component = createComponent();

            // act
            component.instance.requestContainers[0].isChecked = true;
            component.instance.denyAssetTransferRequests();

            // assert
            // TODO: Expand the data values to be more specific.
            expect($modalState.getFor("show")).toHaveBeenCalledWith(
                "#modal-dialog",
                ".deny",
                {
                    data: {
                        requests: jasmine.any(Object),
                        returnLocation: jasmine.any(String),
                        onConfirm: jasmine.any(Function)
                    }
                }
            );
        });
    });

    function createComponent(): ComponentInstance<AssetTransferRequestsComponent> {
        return spec.createComponent<AssetTransferRequestsComponent>({
            markup: `<pcd-asset-transfer-requests></pcd-pcd-asset-transfer-requests>`
        });
    }
});
