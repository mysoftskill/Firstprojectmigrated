import { TestSpec, ComponentInstance } from "../../../../shared-tests/spec.base";
import { RegistrationState, AgentRegistrationStatus, DataAgentWithHealthStatus, HealthIcon }
    from "../../../../shared/registration-status/registration-status-types";
import ViewAgentHealthComponent from "./view-agent-health.component";

describe("ViewAgentHealthComponent", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();
    });

    it("When all asset groups turns Valid and all other statuses are Valid, UpdateStatus should set AssetGroup/Agent to Healthy", () => {
        // arrange
        let agentHealth = <DataAgentWithHealthStatus> {
            registrationStatus: <AgentRegistrationStatus> {
                protocolStatus: RegistrationState.valid,
                capabilityStatus: RegistrationState.valid,
                environmentStatus: RegistrationState.valid,
                isComplete: false,
                assetGroupsStatus: RegistrationState.validButTruncated,
                assetGroups: [
                    {
                        isComplete: true,
                        assetsStatus: RegistrationState.valid
                    },
                    {
                        isComplete: true,
                        assetsStatus: RegistrationState.valid
                    }
                ]
            }
        };

        let component = createComponent(agentHealth);

        // act
        component.instance.updateStatus();

        // assert
        expect(agentHealth.registrationStatus.isComplete).toBeTruthy();
        expect(agentHealth.agentHealthIcon).toBe(HealthIcon.healthy);
        expect(agentHealth.registrationStatus.assetGroupsStatus).toBe(RegistrationState.valid);
    });

    it("When any assetGroup is Unhealthy, UpdateStatus should not update the AssetGroupStatus and should stay Unhealthy", () => {
        // arrange
        let agentHealth = <DataAgentWithHealthStatus> {
            registrationStatus: <AgentRegistrationStatus> {
                protocolStatus: RegistrationState.valid,
                capabilityStatus: RegistrationState.valid,
                environmentStatus: RegistrationState.valid,
                isComplete: false,
                assetGroupsStatus: RegistrationState.validButTruncated,
                assetGroups: [
                    {
                        isComplete: true,
                        assetsStatus: RegistrationState.valid
                    },
                    {
                        isComplete: true,
                        assetsStatus: RegistrationState.valid
                    }
                ]
            }
        };

        let component = createComponent(agentHealth);

        // act
        component.instance.updateStatus();

        // assert
        expect(agentHealth.registrationStatus.isComplete).toBeTruthy();
        expect(agentHealth.agentHealthIcon).toBe(HealthIcon.healthy);
        expect(agentHealth.registrationStatus.assetGroupsStatus).toBe(RegistrationState.valid);
    });

    it("When all assetGroups are valid but other agent level sub statuses Unhealthy, AgentHealth status should stay Unhealthy", () => {
        // arrange
        let agentHealth = <DataAgentWithHealthStatus> {
            registrationStatus: <AgentRegistrationStatus> {
                protocolStatus: RegistrationState.missing,
                capabilityStatus: RegistrationState.valid,
                environmentStatus: RegistrationState.valid,
                isComplete: false,
                assetGroupsStatus: RegistrationState.valid,
                assetGroups: [
                    {
                        isComplete: true,
                        assetsStatus: RegistrationState.valid
                    },
                    {
                        isComplete: true,
                        assetsStatus: RegistrationState.valid
                    }
                ]
            },
            agentHealthIcon: HealthIcon.unhealthy
        };

        let component = createComponent(agentHealth);

        // act
        component.instance.updateStatus();

        // assert
        expect(agentHealth.registrationStatus.isComplete).toBeFalsy();
        expect(agentHealth.agentHealthIcon).toBe(HealthIcon.unhealthy);
        expect(agentHealth.registrationStatus.assetGroupsStatus).toBe(RegistrationState.valid);
    });

    function createComponent(agentHealth: DataAgentWithHealthStatus): ComponentInstance<ViewAgentHealthComponent> {
        return spec.createComponent<ViewAgentHealthComponent>({
            markup: `<pcd-view-agent-health agent-health="agentHealth" display-by-status="all"></pcd-view-agent-health>`,
            data: { agentHealth }
        });
    }
});
