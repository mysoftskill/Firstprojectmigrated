import { TestSpec, SpyCache, ComponentInstance } from "../../../../shared-tests/spec.base";
import * as Pdms from "../../../../shared/pdms/pdms-types";

import PendingSharingRequestComponent from "./pending-sharing-requests.component";

describe("PendingSharingRequestComponent", () => {
    let spec: TestSpec;
    let meeModalServiceMock: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let $stateParams: ng.ui.IStateParamsService;

    let requestObjects: Pdms.SharingRequest[] = [{
        id: "9c5f8fe0-619a-434b-806f-46276ae13de1",
        agentId: "2c22345gh-8f454",
        ownerId: "4cb3c61d-cc1f-44be-ac24-0114de4fcf07",
        ownerName: "OwnerName",
        relationships: [{
            assetGroupId: "1234567890a",
            assetGroupQualifier: {
                props: {
                    AssetType: "CosmosStructuredStream",
                    PhysicalCluster: "guess",
                    VirtualCluster: "mylove",
                    RelativePath: "/local/lul"
                }
            },
            capabilities: ["Delete", "Export"]
        }]
    }, {
        id: "9c5f8fe0-619a-434b-806f-423gf7789",
        agentId: "2c22345gh-8f454",
        ownerId: "4cb-44be-ac24-0114de4fcf07",
        ownerName: "OwnerName2",
        relationships: [{
            assetGroupId: "1234567890avbc",
            assetGroupQualifier: {
                props: {
                    AssetType: "CosmosStructuredStream",
                    PhysicalCluster: "guesses",
                    VirtualCluster: "myloves",
                    RelativePath: "/local/lulz"
                }
            },
            capabilities: ["Delete", "Export"]
        }]
    }];

    let agentObject: Pdms.DataAgent = {
        kind: "delete-agent",
        assetGroups: null,
        id: "2c2a51a1-5d7b-4bfd-8f4f-ca8020549654",
        name: "wer4-tdz",
        description: "agent",
        ownerId: "4cb3c61d-cc1f-44be-ac24-0114de4fcf07",
        sharingEnabled: false,
        isThirdPartyAgent: false,
        hasSharingRequests: true,
        connectionDetails: {
            Prod: {
                releaseState: "",
                protocol: "",
                authenticationType: "",
                msaSiteId: 2345,
                aadAppId: ""
            }
        },
        operationalReadiness: null,
        deploymentLocation: null,
        supportedClouds: null,
        pendingCommandsFound: false,
        dataResidencyBoundary: "Global"
    };

    beforeEach(() => {
        spec = new TestSpec();
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", null);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDeleteAgentById", agentObject);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getSharingRequestsByAgentId", requestObjects);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getOwnersByAuthenticatedUser", {});

        inject(((_$stateParams_: ng.ui.IStateParamsService, _$meeModal_: MeePortal.OneUI.Angular.IModalStateService) => {
            $stateParams = _$stateParams_;
            meeModalServiceMock = new SpyCache(_$meeModal_);
        }));

        meeModalServiceMock.getFor("show").and.stub();
        $stateParams.agentId = "1234567890";
    });

    describe("for approve/deny modals", () => {
        it("calls the approve-sharing-request modal with the required parameters to approve a Sharing Request", () => {
            // arrange
            let component = createComponent();

            // act
            component.instance.requestContainers[0].isChecked = true;
            component.instance.approveSharingRequests();

            // assert
            expect(meeModalServiceMock.getFor("show")).toHaveBeenCalledWith(
                "#modal-dialog",
                ".approve",
                {
                    data: {
                        requests: jasmine.any(Object),
                        onConfirm: jasmine.any(Function)
                    }
                }
            );
        });

        it("calls the deny-sharing-request modal with the required parameters to deny a Sharing Request", () => {
            // arrange
            let component = createComponent();

            // act
            component.instance.requestContainers[0].isChecked = true;
            component.instance.denySharingRequests();

            // assert
            expect(meeModalServiceMock.getFor("show")).toHaveBeenCalledWith(
                "#modal-dialog",
                ".deny",
                {
                    data: {
                        requests: jasmine.any(Object),
                        onConfirm: jasmine.any(Function)
                    }
                }
            );
        });
    });

    function createComponent(): ComponentInstance<PendingSharingRequestComponent> {
        return spec.createComponent<PendingSharingRequestComponent>({
            markup: `<pcd-pending-sharing-requests></pcd-pending-sharing-requests>`
        });
    }
});
