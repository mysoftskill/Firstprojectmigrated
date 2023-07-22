import { TestSpec, SpyCache, ComponentInstance } from "../../../../shared-tests/spec.base";
import * as VariantApi from "../../../../shared/variant/variant-types";

import { IVariantDataService } from "../../../../shared/variant/variant-data.service";
import VariantRequestDetailsComponent from "./variant-request-details.component";

describe("VariantRequestDetailsComponent", () => {
    let spec: TestSpec;
    let variantDataService: IVariantDataService;
    let meeModalServiceMock: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let $stateParams: ng.ui.IStateParamsService;

    let request: VariantApi.VariantRequest = {
        id: "359603",
        ownerId: "4cb-44be-ac24-0114de4fcf07",
        ownerName: "OwnerName",
        trackingDetails: {
            createdOn: null
        },
        requestedVariants: [{
            variantId: "1234567890",
            variantName: "MockVariant1",
            tfsTrackingUris: ["www.test.com"],
            disabledSignalFiltering: false,
            variantExpiryDate: null,
            variantState: null
        }],
        variantRelationships: [{
            assetGroupId: "1234567890a",
            assetGroupQualifier: {
                props: {
                    AssetType: "CosmosStructuredStream",
                    PhysicalCluster: "guess",
                    VirtualCluster: "mylove",
                    RelativePath: "/local/lul"
                }
            }
        }]
    };

    let requests: VariantApi.VariantRequest[] = [request];

    beforeEach(() => {
        spec = new TestSpec();
        spec.dataServiceMocks.pdmsDataService.getFor("getDataOwnerWithServiceTree").and.stub();
        spec.dataServiceMocks.variantDataService.mockAsyncResultOf("getVariantRequestById", request);
        spec.dataServiceMocks.variantDataService.mockAsyncResultOf("getVariantRequestsByOwnerId", requests);

        inject(((_$stateParams_: ng.ui.IStateParamsService, _$meeModal_: MeePortal.OneUI.Angular.IModalStateService) => {
            $stateParams = _$stateParams_;
            meeModalServiceMock = new SpyCache(_$meeModal_);
        }));

        meeModalServiceMock.getFor("show").and.stub();
        $stateParams.variantRequestId = "359603";
    });

    it("calls the delete-variant-request modal with the required parameters to remove a variant request", () => {
        // arrange
        let component = createComponent();

        // act
        component.instance.showDeleteVariantRequestConfirmationDialog();

        // assert
        expect(meeModalServiceMock.getFor("show")).toHaveBeenCalledWith(
            "#modal-dialog",
            ".delete",
            {
                data: {
                    variantRequest: jasmine.any(Object),
                    onConfirm: jasmine.any(Function),
                    returnLocation: jasmine.any(String),
                    returnLocationOnCancel: jasmine.any(String)
                }
            }
        );
    });

    function createComponent(): ComponentInstance<VariantRequestDetailsComponent> {
        return spec.createComponent<VariantRequestDetailsComponent>({
            markup: `<pcd-variant-request-details></pcd-variant-request-details>`
        });
    }
});
