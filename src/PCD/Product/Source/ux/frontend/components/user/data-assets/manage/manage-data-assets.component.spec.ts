import { TestSpec, SpyCache, ComponentInstance } from "../../../../shared-tests/spec.base";
import * as Pdms from "../../../../shared/pdms/pdms-types";

import ManageDataAssetsComponent from "./manage-data-assets.component";

describe("ManageDataAssetsComponent", () => {
    let spec: TestSpec;

    let $modalState: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let $stateParams: ng.ui.IStateParamsService;

    let assetGroups: Pdms.AssetGroup[] = [{
        id: "DataAsset1ID",
        hasPendingVariantRequests: false,
        hasPendingTransferRequest: true,
        qualifier: {
            props: {
                AssetType: "DataAsset1Type",
                propName: "propValue"
            }
        }
    }];

    let owner: Pdms.DataOwner = {
        id: "ownerID",
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
        spec = new TestSpec();
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getAssetGroupsByOwnerId", assetGroups);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owner);
        spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", null);
        spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getSecurityGroupById", null);

        inject((
            _$stateParams_: ng.ui.IStateParamsService,
            _$meeModal_: MeePortal.OneUI.Angular.IModalStateService,
        ) => {
            $stateParams = _$stateParams_;
            $modalState = new SpyCache(_$meeModal_);
        });

        $stateParams.ownerId = "1234567890";
    });

    it("hasPendingTransferRequests returns true when the an assetGroup has a pending transfer requests", () => {
        // arrange
        let component = createComponent();

        // act
        let result = component.instance.hasPendingTransferRequests();

        // assert
        expect(result).toBeTruthy();
    });

    function createComponent(): ComponentInstance<ManageDataAssetsComponent> {
        return spec.createComponent<ManageDataAssetsComponent>({
            markup: `<pcd-manage-data-assets></pcd-manage-data-assets>`
        });
    }
});
