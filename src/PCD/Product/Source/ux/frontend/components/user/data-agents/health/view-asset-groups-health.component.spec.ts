import ViewAssetGroupsHealthComponent from "./view-asset-groups-health.component";
import { ComponentInstance, TestSpec, SpyCache } from "../../../../shared-tests/spec.base";
import { AssetGroupRegistrationStatus, RegistrationState, DataAgentWithHealthStatus, AgentRegistrationStatus } from "../../../../shared/registration-status/registration-status-types";
import ViewAgentHealthComponent from "./view-agent-health.component";

describe("ViewAssetGroupsHealthComponent", () => {
    let spec: TestSpec;
    let assetGroupsStatus: AssetGroupRegistrationStatus[];
    let meeComponentRegistrySpy: SpyCache<MeePortal.OneUI.Angular.IMeeComponentRegistryService>;

    beforeEach(() => {
        spec = new TestSpec();
        meeComponentRegistrySpy = new SpyCache(spec.$meeComponentRegistry);
    });

    describe("Owner name to display", () => {
        it("shows no owner without an owner name", () => {
            // arrange / act
            assetGroupsStatus = [
                {
                    id: "any",
                    ownerId: "owner1",
                    ownerName: null,
                    isComplete: false,
                    qualifier: null,
                    assets: null,
                    assetsStatus: RegistrationState.invalid,
                }
            ];
            meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(createViewAgentHealthComponent(assetGroupsStatus));

            let component = createComponent(assetGroupsStatus, "owner1", "all");

            // assert
            expect(component.instance.shouldShowNoOwner(assetGroupsStatus)).toBe(true);
        });

        it("shows owner contact when showing all statuses", () => {
            // arrange / act
            assetGroupsStatus = [
                {
                    id: "any",
                    ownerId: "owner1",
                    ownerName: "owner1",
                    isComplete: true,
                    qualifier: null,
                    assets: null,
                    assetsStatus: RegistrationState.valid,
                }
            ];
            meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(createViewAgentHealthComponent(assetGroupsStatus));

            let component = createComponent(assetGroupsStatus, "owner1", "all");

            // assert
            expect(component.instance.shouldShowOwnerContact(assetGroupsStatus)).toBe(true);
        });

        it("shows owner contact when filtered by issues with issues on the data assets", () => {
            // arrange / act
            assetGroupsStatus = [
                {
                    id: "any",
                    ownerId: "owner1",
                    ownerName: "owner1",
                    isComplete: false,
                    qualifier: null,
                    assets: null,
                    assetsStatus: RegistrationState.valid,
                }
            ];
            meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(createViewAgentHealthComponent(assetGroupsStatus));

            let component = createComponent(assetGroupsStatus, "owner1", "issues");

            // assert
            expect(component.instance.shouldShowOwnerContact(assetGroupsStatus)).toBe(true);
        });

        it("shows no owner contact when filtered by issues without any issues on the data assets and no owner name", () => {
            // arrange / act
            assetGroupsStatus = [
                {
                    id: "any",
                    ownerId: "owner1",
                    ownerName: null,
                    isComplete: true,
                    qualifier: null,
                    assets: null,
                    assetsStatus: RegistrationState.valid,
                }
            ];
            meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(createViewAgentHealthComponent(assetGroupsStatus));

            let component = createComponent(assetGroupsStatus, "owner1", "issues");

            // assert
            expect(component.instance.shouldShowOwnerContact(assetGroupsStatus)).toBe(false);
        });
    });

    function createComponent(assetGroupRegistrationStatus: AssetGroupRegistrationStatus[],
                             agentOwnerId: string,
                             displayBy: string): ComponentInstance<ViewAssetGroupsHealthComponent> {
        return spec.createComponent<ViewAssetGroupsHealthComponent>({
            markup: `<pcd-view-asset-groups-health
                pcd-asset-groups="assetGroupRegistrationStatus"
                pcd-agent-owner-id="agentOwnerId"
                display-by-status="{{displayBy}}"></pcd-view-asset-groups-health>`,
            data: { assetGroupRegistrationStatus, agentOwnerId, displayBy }
        });
    }

    function createViewAgentHealthComponent(assetGroupRegistrationStatus: AssetGroupRegistrationStatus[]): ComponentInstance<ViewAgentHealthComponent> {
        let agentHealth = <DataAgentWithHealthStatus> {
            registrationStatus: <AgentRegistrationStatus> {
                protocolStatus: RegistrationState.valid,
                capabilityStatus: RegistrationState.valid,
                environmentStatus: RegistrationState.valid,
                isComplete: false,
                assetGroupsStatus: RegistrationState.validButTruncated,
                assetGroups: assetGroupRegistrationStatus
            }
        };

        return spec.createComponent<ViewAgentHealthComponent>({
            markup: `<pcd-view-agent-health agent-health="agentHealth" display-by-status="all"></pcd-view-agent-health>`,
            data: { agentHealth }
        });
    }
});
