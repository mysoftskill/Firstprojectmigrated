import { TestSpec, ComponentInstance } from "../../../../shared-tests/spec.base";
import SelectDataAgent from "./manage-data-agents.component";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import * as GraphTypes from "../../../../shared/graph/graph-types";

import { SpyCache } from "../../../../shared-tests/spy-cache";

describe("ManageDataAgents", () => {
    let spec: TestSpec;

    let dataOwner: Pdms.DataOwner;
    let dataAgents: Pdms.DeleteAgent[];

    let $modalState: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((
            _$meeModal_: MeePortal.OneUI.Angular.IModalStateService
        ) => {
            $modalState = new SpyCache(_$meeModal_);
        });

        dataAgents = [{
            kind: "delete-agent",
            assetGroups: null,
            id: "2c2a51a1-5d7b-4bfd-8f4f-ca8020549654",
            name: "wer4-tdz",
            description: "agent",
            ownerId: "4cb3c61d-cc1f-44be-ac24-0114de4fcf07",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            hasSharingRequests: false,
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
        }, {
            kind: "delete-agent",
            assetGroups: null,
            id: "2c2a51a1-5d7b-4bfd-8f4f-ca8087489237",
            name: "wer4-tdz",
            description: "agent",
            ownerId: "4cb3c61d-cc1f-44be-ac24-01223jdhf821",
            sharingEnabled: false,
            isThirdPartyAgent: false,
            hasSharingRequests: true,
            connectionDetails: {},
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
            pendingCommandsFound: false,
            dataResidencyBoundary: "Global"
        }];

        dataOwner = {
            id: "07b5b571-81e3-4aca-bde8-7ddec0c98adc",
            name: "owner1",
            description: "Described",
            alertContacts: ["abc@microsoft.com"],
            announcementContacts: ["abc@microsoft.com"],
            sharingRequestContacts: ["abc@microsoft.com"],
            assetGroups: [],
            dataAgents: dataAgents,
            writeSecurityGroups: ["abc@microsoft.com"],
            serviceTree: null,
            tagSecurityGroups: []
        };

        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataAgentsByOwnerId", dataAgents);
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataOwnerWithServiceTree", dataOwner);
        spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", null);
        spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getSecurityGroupById", null);
    });

    it("initializes state correctly", () => {
        // act
        let component = createComponent();

        // assert
        expect(spec.dataServiceMocks.pdmsDataService.getFor("getDataAgentsByOwnerId")).toHaveBeenCalled();

        expect(component.instance.dataAgents).toEqual(dataAgents);
    });

    it("calls to agentWithSharingRequestsExist returns true when some Data Agent has sharing requests.", () => {
        // arrange
        let component = createComponent();

        // act assert
        expect(component.instance.agentWithSharingRequestsExist()).toEqual(true);
    });


    function createComponent(): ComponentInstance<SelectDataAgent> {
        return spec.createComponent<SelectDataAgent>({
            markup: `<pcd-manage-data-agents></pcd-manage-data-agents>`
        });
    }
});
