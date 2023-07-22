import { TestSpec, ComponentInstance } from "../../../../shared-tests/spec.base";
import LinkDataAgentComponent from "./link-data-agent.component";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import { SpyCache } from "../../../../shared-tests/spy-cache";
import { DataAgentLinkingContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { IPdmsDataService } from "../../../../shared/pdms/pdms-types";
import { SetAgentRelationshipRequest, Capability, ActionVerb } from "../../../../shared/pdms-agent-relationship-types";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";

describe("LinkDataAgent", () => {
    let spec: TestSpec;
    let $modalState: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
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
    });

    describe("linking scenarios", () => {
        let assetGroups: Pdms.AssetGroup[];
        let modalData: DataAgentLinkingContext;

        beforeEach(() => {
            assetGroups = [{
                id: "DataAsset1ID",
                qualifier: {
                    props: {
                        AssetType: "DataAsset1Type",
                        propName: "propValue"
                    }
                },
                ownerId: "1",
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
            }];

            modalData = {
                agentId: "agent1",
                agentName: "sample delete agent",
                assetGroups: assetGroups,
                verb: ActionVerb.set,
                onComplete: () => {}
            };

            $modalState.getFor("getData").and.returnValue(modalData);
        });

        it("parameters are correctly populated", () => {
            //arrange
            let component = createComponent();
            let pdmsDataService = spec.dataServiceMocks.pdmsDataService;
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("setAgentRelationshipsAsync");

            component.instance.deleteSelected = true;
            component.instance.exportSelected = true;

            expect(component.instance.hasAcknowledgedUnlink).toBe(false);
            expect(component.instance.isUnlinkFlow()).toBe(false);

            //act
            component.instance.linkOrUnlink();

            //assert
            expect(pdmsDataService.getFor("setAgentRelationshipsAsync")).toHaveBeenCalled();
            let args = pdmsDataService.getFor("setAgentRelationshipsAsync").calls.argsFor(0);
            let request = <SetAgentRelationshipRequest> args[0];

            expect(request.relationships.length).toEqual(2);
            expect(request.relationships[0].assetGroupId).toEqual(modalData.assetGroups[0].id);
            expect(request.relationships[0].assetGroupETag).toEqual(modalData.assetGroups[0].eTag);
            expect(request.relationships[0].actions.length).toEqual(2);
            expect(request.relationships[0].actions[0].capability).toEqual(Capability.export);
            expect(request.relationships[0].actions[0].verb).toEqual(ActionVerb.set);
            expect(request.relationships[0].actions[0].agentId).toEqual(modalData.agentId);
            expect(request.relationships[0].actions[1].capability).toEqual(Capability.delete);
            expect(request.relationships[0].actions[1].verb).toEqual(ActionVerb.set);
            expect(request.relationships[0].actions[1].agentId).toEqual(modalData.agentId);

            expect(request.relationships[1].assetGroupId).toEqual(modalData.assetGroups[1].id);
            expect(request.relationships[1].assetGroupETag).toEqual(modalData.assetGroups[1].eTag);
            expect(request.relationships[1].actions.length).toEqual(2);
            expect(request.relationships[1].actions[0].capability).toEqual(Capability.export);
            expect(request.relationships[1].actions[0].verb).toEqual(ActionVerb.set);
            expect(request.relationships[1].actions[0].agentId).toEqual(modalData.agentId);
            expect(request.relationships[1].actions[1].capability).toEqual(Capability.delete);
            expect(request.relationships[1].actions[1].verb).toEqual(ActionVerb.set);
            expect(request.relationships[1].actions[1].agentId).toEqual(modalData.agentId);
        });

        it("on successful link, modal is switched to warning prompt", () => {
            //arrange
            let component = createComponent();

            $modalState.getFor("switchTo").and.stub();
            spyOn(modalData, "onComplete").and.stub();
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("setAgentRelationshipsAsync");

            component.instance.deleteSelected = true;
            component.instance.exportSelected = true;

            //act
            component.instance.linkOrUnlink();

            //assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("setAgentRelationshipsAsync")).toHaveBeenCalled();

            spec.runDigestCycle();

            expect($modalState.getFor("switchTo")).toHaveBeenCalledWith("^.ngp-warning-prompt");
            expect(modalData.onComplete).toHaveBeenCalled();
        });

        it("when linking fails, user see a error message", () => {
            //arrange
            let component = createComponent();

            pcdErrorService.getFor("setError").and.stub();
            spec.dataServiceMocks.pdmsDataService.mockFailureOf("setAgentRelationshipsAsync");

            component.instance.deleteSelected = true;
            component.instance.exportSelected = true;

            //act
            component.instance.linkOrUnlink();

            //assert
            expect(spec.dataServiceMocks.pdmsDataService.getFor("setAgentRelationshipsAsync")).toHaveBeenCalled();

            spec.runDigestCycle();

            expect(pcdErrorService.getFor("setError")).toHaveBeenCalled();
        });

        describe("disallowedToLinkOrUnlink", () => {
            let component: ComponentInstance<LinkDataAgentComponent>;

            beforeEach(() => {
                component = createComponent();
            });

            it("returns true, if no privacy actions selected", () => {
                // arrange
                component.instance.deleteSelected = false;
                component.instance.exportSelected = false;

                // act
                expect(component.instance.disallowedToLinkOrUnlink()).toBe(true);
            });

            it("returns false, if at least delete action is selected", () => {
                // arrange
                component.instance.deleteSelected = true;
                component.instance.exportSelected = false;

                // act
                expect(component.instance.disallowedToLinkOrUnlink()).toBe(false);
            });

            it("returns false, if at least export action is selected", () => {
                // arrange
                component.instance.deleteSelected = true;
                component.instance.exportSelected = false;

                // act
                expect(component.instance.disallowedToLinkOrUnlink()).toBe(false);
            });
        });
    });

    describe("unlinking scenarios", () => {
        let assetGroups: Pdms.AssetGroup[];
        let modalData: DataAgentLinkingContext;

        beforeEach(() => {
            assetGroups = [{
                id: "DataAsset1ID",
                qualifier: {
                    props: {
                        AssetType: "DataAsset1Type",
                        propName: "propValue"
                    }
                },
                ownerId: "1",
                deleteAgentId: "DataAgent1ID",
                exportAgentId: "DataAgent2ID"
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
                deleteAgentId: "DataAgent2ID",
                exportAgentId: "DataAgent1ID"
            }];

            modalData = {
                agentId: "DataAgent1ID",
                agentName: "sample delete agent",
                assetGroups: assetGroups,
                verb: ActionVerb.clear,
                onComplete: () => {}
            };

            $modalState.getFor("getData").and.returnValue(modalData);
        });

        it("parameters are correctly populated when agentId in context", () => {
            //arrange
            let component = createComponent();
            let pdmsDataService = spec.dataServiceMocks.pdmsDataService;
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("setAgentRelationshipsAsync");

            component.instance.deleteSelected = true;
            component.instance.exportSelected = true;

            expect(component.instance.hasAcknowledgedUnlink).toBe(false);
            expect(component.instance.isUnlinkFlow()).toBe(true);

            component.instance.hasAcknowledgedUnlink = true;

            //act
            component.instance.linkOrUnlink();

            //assert
            expect(pdmsDataService.getFor("setAgentRelationshipsAsync")).toHaveBeenCalled();
            let args = pdmsDataService.getFor("setAgentRelationshipsAsync").calls.argsFor(0);
            let request = <SetAgentRelationshipRequest> args[0];

            expect(request.relationships.length).toEqual(2);
            expect(request.relationships[0].assetGroupId).toEqual(modalData.assetGroups[0].id);
            expect(request.relationships[0].assetGroupETag).toEqual(modalData.assetGroups[0].eTag);
            expect(request.relationships[0].actions.length).toEqual(1);
            expect(request.relationships[0].actions[0].capability).toEqual(Capability.delete);
            expect(request.relationships[0].actions[0].verb).toEqual(ActionVerb.clear);
            expect(request.relationships[0].actions[0].agentId).toBeNull();

            expect(request.relationships[1].assetGroupId).toEqual(modalData.assetGroups[1].id);
            expect(request.relationships[1].assetGroupETag).toEqual(modalData.assetGroups[1].eTag);
            expect(request.relationships[1].actions.length).toEqual(1);
            expect(request.relationships[1].actions[0].capability).toEqual(Capability.export);
            expect(request.relationships[1].actions[0].verb).toEqual(ActionVerb.clear);
            expect(request.relationships[1].actions[0].agentId).toBeNull();
        });

        it("parameters are correctly populated when agentId not in context", () => {
            //arrange
            let component = createComponent();
            let pdmsDataService = spec.dataServiceMocks.pdmsDataService;
            modalData.agentId = null;
            spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("setAgentRelationshipsAsync");

            component.instance.deleteSelected = true;
            component.instance.exportSelected = true;

            //act
            component.instance.linkOrUnlink();

            //assert
            expect(pdmsDataService.getFor("setAgentRelationshipsAsync")).toHaveBeenCalled();
            let args = pdmsDataService.getFor("setAgentRelationshipsAsync").calls.argsFor(0);
            let request = <SetAgentRelationshipRequest> args[0];

            expect(request.relationships.length).toEqual(2);
            expect(request.relationships[0].assetGroupId).toEqual(modalData.assetGroups[0].id);
            expect(request.relationships[0].assetGroupETag).toEqual(modalData.assetGroups[0].eTag);
            expect(request.relationships[0].actions.length).toEqual(2);
            expect(request.relationships[0].actions[0].capability).toEqual(Capability.export);
            expect(request.relationships[0].actions[0].verb).toEqual(ActionVerb.clear);
            expect(request.relationships[0].actions[0].agentId).toBeNull();
            expect(request.relationships[0].actions[1].capability).toEqual(Capability.delete);
            expect(request.relationships[0].actions[1].verb).toEqual(ActionVerb.clear);
            expect(request.relationships[0].actions[1].agentId).toBeNull();

            expect(request.relationships[1].assetGroupId).toEqual(modalData.assetGroups[1].id);
            expect(request.relationships[1].assetGroupETag).toEqual(modalData.assetGroups[1].eTag);
            expect(request.relationships[1].actions.length).toEqual(2);
            expect(request.relationships[1].actions[0].capability).toEqual(Capability.export);
            expect(request.relationships[1].actions[0].verb).toEqual(ActionVerb.clear);
            expect(request.relationships[1].actions[0].agentId).toBeNull();
            expect(request.relationships[1].actions[1].capability).toEqual(Capability.delete);
            expect(request.relationships[1].actions[1].verb).toEqual(ActionVerb.clear);
            expect(request.relationships[1].actions[1].agentId).toBeNull();
        });

        describe("disallowedToLinkOrUnlink", () => {
            let component: ComponentInstance<LinkDataAgentComponent>;

            beforeEach(() => {
                component = createComponent();
            });

            it("returns true, if no privacy actions selected", () => {
                // arrange
                component.instance.deleteSelected = false;
                component.instance.exportSelected = false;
                component.instance.hasAcknowledgedUnlink = true;

                // act
                expect(component.instance.disallowedToLinkOrUnlink()).toBe(true);
            });

            it("returns true, if user did not acknowledge unlink operation", () => {
                // arrange
                component.instance.deleteSelected = true;
                component.instance.exportSelected = true;
                component.instance.hasAcknowledgedUnlink = false;

                // act
                expect(component.instance.disallowedToLinkOrUnlink()).toBe(true);
            });

            it("returns false, if at least delete action is selected", () => {
                // arrange
                component.instance.deleteSelected = true;
                component.instance.exportSelected = false;
                component.instance.hasAcknowledgedUnlink = true;

                // act
                expect(component.instance.disallowedToLinkOrUnlink()).toBe(false);
            });

            it("returns false, if at least export action is selected", () => {
                // arrange
                component.instance.deleteSelected = true;
                component.instance.exportSelected = false;
                component.instance.hasAcknowledgedUnlink = true;

                // act
                expect(component.instance.disallowedToLinkOrUnlink()).toBe(false);
            });
        });
    });

    function createComponent(): ComponentInstance<LinkDataAgentComponent> {
        return spec.createComponent<LinkDataAgentComponent>({
            markup: `<pcd-link-data-agent></pcd-link-data-agent>`
        });
    }
});
