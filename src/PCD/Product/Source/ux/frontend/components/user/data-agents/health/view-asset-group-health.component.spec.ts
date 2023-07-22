import { TestSpec, ComponentInstance, SpyCache } from "../../../../shared-tests/spec.base";
import ViewAssetGroupHealthComponent from "./view-asset-group-health.component";
import { RegistrationState, AssetGroupRegistrationStatus, HealthIcon, AgentRegistrationStatus, DataAgentWithHealthStatus }
    from "../../../../shared/registration-status/registration-status-types";
import ViewAgentHealthComponent from "./view-agent-health.component";

describe("ViewAssetGroupHealthComponent", () => {
    let spec: TestSpec;
    let meeComponentRegistrySpy: SpyCache<MeePortal.OneUI.Angular.IMeeComponentRegistryService>;

    beforeEach(() => {
        spec = new TestSpec();
        meeComponentRegistrySpy = new SpyCache(spec.$meeComponentRegistry);
    });

    it("When AssetGroup isComplete is true, sets icon to Healthy", () => {
        // arrange
        let assetGroupHealth = <AssetGroupRegistrationStatus> {
            isComplete: true,
            assetsStatus: RegistrationState.valid,
        };
        meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(createViewAgentHealthComponent([assetGroupHealth]));

        let component = createComponent(assetGroupHealth);

        // act/assert
        expect(component.instance.getIconState()).toBe(HealthIcon.healthy);
    });

    it("When status is ValidButTruncated with no assets, sets icon to Unknown and show Load link", () => {
        // arrange
        let assetGroupHealth = <AssetGroupRegistrationStatus> {
            isComplete: false,
            assetsStatus: RegistrationState.validButTruncated,
            assets: []
        };
        meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(createViewAgentHealthComponent([assetGroupHealth]));

        let component = createComponent(assetGroupHealth);

        // act/assert
        expect(component.instance.getIconState()).toBe(HealthIcon.unknown);
        expect(component.instance.shouldShowLoadLink()).toBeTruthy();
    });

    it("When status is ValidButTruncated with assets, sets icon to Unhealthy ", () => {
        // arrange
        let assetGroupHealth = <AssetGroupRegistrationStatus> {
            isComplete: false,
            assetsStatus: RegistrationState.validButTruncated,
            assets: [{ }]
        };
        meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(createViewAgentHealthComponent([assetGroupHealth]));

        let component = createComponent(assetGroupHealth);

        // act/assert
        expect(component.instance.getIconState()).toBe(HealthIcon.unhealthy);
    });

    function createComponent(assetGroup: AssetGroupRegistrationStatus): ComponentInstance<ViewAssetGroupHealthComponent> {
        return spec.createComponent<ViewAssetGroupHealthComponent>({
            markup: `<pcd-view-asset-group-health asset-group="assetGroup" display-by-status="all"></pcd-view-asset-group-health>`,
            data: { assetGroup }
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
