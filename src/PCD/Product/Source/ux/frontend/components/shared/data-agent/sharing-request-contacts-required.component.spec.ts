import { TestSpec, SpyCache, ComponentInstance } from "../../../shared-tests/spec.base";
import * as Pdms from "../../../shared/pdms/pdms-types";

import SharingRequestContactsRequiredComponent from "./sharing-request-contacts-required.component";

describe("SharingRequestContactsRequiredComponent", () => {
    let spec: TestSpec;
    let meeModalServiceMock: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;

    let ownerObjectInitial: Pdms.DataOwner = {
        id: "b9cc0d10-e200-4543-8469-7984398389343c8b",
        name: "TestOwner",
        description: "BlahblahblahBlahblahblahBlahblahblah",
        alertContacts: [],
        announcementContacts: [],
        sharingRequestContacts: [],
        assetGroups: null,
        dataAgents: null,
        writeSecurityGroups: ["22eae02b-16cf-4fa1-b496-08eef042938742"],
        serviceTree: null
    };

    let ownerObjectFinal: Pdms.DataOwner = {
        id: "b9cc0d10-e200-4543-8469-7984398389343c8b",
        name: "TestOwner",
        description: "BlahblahblahBlahblahblahBlahblahblah",
        alertContacts: [],
        announcementContacts: [],
        sharingRequestContacts: ["someone@microsoft.com"],
        assetGroups: null,
        dataAgents: null,
        writeSecurityGroups: ["22eae02b-16cf-4fa1-b496-08eef042938742"],
        serviceTree: null
    };

    let returnLocation = "data-agents.edit";

    beforeEach(() => {
        spec = new TestSpec();
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("updateDataOwner", ownerObjectInitial);

        inject(((_$meeModal_: MeePortal.OneUI.Angular.IModalStateService) => {
            meeModalServiceMock = new SpyCache(_$meeModal_);
        }));

        meeModalServiceMock.getFor("getData").and.returnValue({
            owner: ownerObjectInitial,
            returnLocation: returnLocation
        });
    });

    describe("for onSave", () => {
        it("calls to onSave succeed with valid data.", () => {
            // arrange
            let component = createComponent();

            // act
            component.instance.sharingRequestContactsSelectorData.contacts.push({
                id: "1234567890",
                email: "someone@microsoft.com",
                displayName: "Someone",
                isInvalid: false,
            });
            component.instance.onSave();

            // assert
            expect(component.instance.owner).toEqual(ownerObjectFinal);
            expect(spec.dataServiceMocks.pdmsDataService.getFor("updateDataOwner")).toHaveBeenCalledWith(component.instance.owner);
        });

        it("calls to onSave fail as a result of empty sharingRequestContacts field", () => {
            // arrange
            let component = createComponent();

            // act
            component.instance.onSave();

            // assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("updateDataOwner")).not.toHaveBeenCalled();
        });

    });

    function createComponent(): ComponentInstance<SharingRequestContactsRequiredComponent> {
        return spec.createComponent<SharingRequestContactsRequiredComponent>({
            markup: `<pcd-sharing-request-contacts-required></pcd-sharing-request-contacts-required>`
        });
    }
});