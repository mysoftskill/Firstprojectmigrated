import { TestSpec, SpyCache, ComponentInstance } from "../../../shared-tests/spec.base";
import * as Pdms from "../../../shared/pdms/pdms-types";

import LandingDashboardComponent from "./landing-dashboard.component";

describe("LandingDashboardComponent", () => {
    let spec: TestSpec;

    let owners: Pdms.DataOwner[] = [{
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
    }];

    beforeEach(() => {
        spec = new TestSpec();
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getOwnersByAuthenticatedUser", owners);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getAssetGroupsCountByOwnerId", 10);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataAgentsCountByOwnerId", 5);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", owners[0]);
    });

    describe("LandingDashboardComponent", () => {
        it("initializes state correctly", () => {
            // arrange
            let component = createComponent();

            // assert
            expect(component.instance.teamPickerModel.owners).toEqual(owners);
            expect(component.instance.teamPickerModel.selectedOwnerId).toEqual("ownerID");
            expect(component.instance.agentsCount).toEqual(5);
            expect(component.instance.assetGroupsCount).toEqual(10);
        });

        it("hasPendingTransferRequests returns true when the selected owner has pending transfer requests", () => {
            // arrange
            let component = createComponent();

            // act
            let result = component.instance.hasPendingTransferRequests();

            // assert
            expect(result).toBeTruthy();
        });
    });

    function createComponent(): ComponentInstance<LandingDashboardComponent> {
        return spec.createComponent<LandingDashboardComponent>({
            markup: `<pcd-landing></pcd-landing>`
        });
    }
});
