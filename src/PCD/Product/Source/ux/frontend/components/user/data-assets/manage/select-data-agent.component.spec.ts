import { TestSpec, ComponentInstance } from "../../../../shared-tests/spec.base";
import SelectDataAgent from "./select-data-agent.component";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import { SpyCache } from "../../../../shared-tests/spy-cache";
import { DataAgentLinkingContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { IPdmsDataService } from "../../../../shared/pdms/pdms-types";
import { SetAgentRelationshipRequest, Capability, ActionVerb } from "../../../../shared/pdms-agent-relationship-types";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";

describe("SelectDataAgent", () => {
    let spec: TestSpec;
    let assetGroups: Pdms.AssetGroup[];
    let dataAgents: Pdms.DeleteAgent[];
    let $modalState: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let modalData: DataAgentLinkingContext;
    let pcdErrorService: SpyCache<IPcdErrorService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((
                _$meeModal_: MeePortal.OneUI.Angular.IModalStateService,
                _pcdErrorService_: IPcdErrorService
            ) => {
                $modalState = new SpyCache(_$meeModal_);
                pcdErrorService = new SpyCache(_pcdErrorService_);
        });
        
        assetGroups = [];
        
        dataAgents = [
            {
                id: "1",
                name: "agent 1",
                sharingEnabled: false,
                isThirdPartyAgent: false,
                hasSharingRequests: false,
                ownerId: "1",
                assetGroups: null,
                description: "agent 1 description",
                kind: "delete-agent",
                connectionDetails: null,
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            },
            {
                id: "2",
                name: "agent 2",
                sharingEnabled: true,
                isThirdPartyAgent: false,
                hasSharingRequests: false,
                ownerId: "2",
                assetGroups: null,
                description: "agent 2 description",
                kind: "delete-agent",
                connectionDetails: null,
                operationalReadiness: null,
                deploymentLocation: null,
                supportedClouds: null,
                pendingCommandsFound: false,
                dataResidencyBoundary: "Global"
            },
        ];
        
        modalData = {
            agentId: "agent1",
            agentName: "sample delete agent",
            assetGroups: assetGroups,
            verb: ActionVerb.set,
            onComplete: () => {}
        };

        $modalState.getFor("getData").and.returnValue(modalData);
    });

    it("data agents maps to SelectList ", () => {
        //arrange
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getSharedDataAgentsByOwnerId", dataAgents);
        let pdmsDataService = spec.dataServiceMocks.pdmsDataService;

        //act
        let component = createComponent();

        //assert
        expect(pdmsDataService.getFor("getSharedDataAgentsByOwnerId")).toHaveBeenCalled();
        expect(component.instance.dataAgentListPickerModel.selectedId).toEqual(dataAgents[0].id);

        expect(component.instance.dataAgentListPickerModel.items[0].id).toEqual(dataAgents[0].id);
        expect(component.instance.dataAgentListPickerModel.items[0].label).toEqual(dataAgents[0].name);

        expect(component.instance.dataAgentListPickerModel.items[1].id).toEqual(dataAgents[1].id);
        expect(component.instance.dataAgentListPickerModel.items[1].label).toEqual(dataAgents[1].name + " (Shared)");
    });
    
    it("show error message when data agents failed to load", () => {
        //arrange
        spec.dataServiceMocks.pdmsDataService.mockFailureOf("getSharedDataAgentsByOwnerId");
        pcdErrorService.getFor("setErrorForId").and.stub();
        let pdmsDataService = spec.dataServiceMocks.pdmsDataService;

        //act
        let component = createComponent();

        //assert
        expect(pdmsDataService.getFor("getSharedDataAgentsByOwnerId")).toHaveBeenCalled();
        expect(pcdErrorService.getFor("setErrorForId")).toHaveBeenCalled();
    });
    
    function createComponent(): ComponentInstance<SelectDataAgent> {
        return spec.createComponent<SelectDataAgent>({
            markup: `<pcd-select-data-agent></pcd-select-data-agent>`
        });
    }
});
