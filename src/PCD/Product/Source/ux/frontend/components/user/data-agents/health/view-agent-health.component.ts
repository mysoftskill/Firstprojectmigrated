import { Component, Inject } from "../../../../module/app.module";
import template = require("./view-agent-health.html!text");

import { DataAgentWithHealthStatus, AssetRegistrationStatus, Tag, RegistrationState, HealthIcon }
    from "../../../../shared/registration-status/registration-status-types";
import { StringUtilities } from "../../../../shared/string-utilities";
import { HealthFilterType } from "./view-agents-health.component";

const useCmsHere_NoProtocols = "None";
const useCmsHere_NoCapabilities = "None";
const useCmsHere_NoEnvironments = "None";
const useCmsHere_NoDataTypes = "None";
const useCmsHere_NoSubjectTypes = "None";

type TagsSelector = (asset: AssetRegistrationStatus) => Tag[];

@Component({
    name: "pcdViewAgentHealth",
    options: {
        template,
        bindings: {
            agentHealth: "<agentHealth",
            displayByStatus: "@",
        }
    }
})
@Inject("$meeComponentRegistry")
export default class ViewAgentHealthComponent implements ng.IComponentController {
    public agentHealth: DataAgentWithHealthStatus;
    public displayByStatus: HealthFilterType;

    public constructor(private $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService) {
        this.$meeComponentRegistry.register("ViewAgentHealthComponent", "ViewAgentHealthComponent", this);
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister("ViewAgentHealthComponent");
    }

    public getProtocols(): string {
        return StringUtilities.getCommaSeparatedList(this.agentHealth.registrationStatus.protocols, {}, useCmsHere_NoProtocols);
    }

    public getCapabilities(): string {
        return StringUtilities.getCommaSeparatedList(this.agentHealth.registrationStatus.capabilities, {}, useCmsHere_NoCapabilities);
    }

    public getDataTypes(): string {
        return StringUtilities.getCommaSeparatedList(this.getAssetsTags(a => a.dataTypeTags), {}, useCmsHere_NoDataTypes);
    }

    public getSubjectTypes(): string {
        return StringUtilities.getCommaSeparatedList(this.getAssetsTags(a => a.subjectTypeTags), {}, useCmsHere_NoSubjectTypes);
    }

    private getAssetsTags(tagsSelector: TagsSelector): string[] {
        return _.unique(_.flatten(
            _.map(this.agentHealth.registrationStatus.assetGroups, ag =>
                _.map(ag.assets, (a: AssetRegistrationStatus) =>
                    _.map(tagsSelector(a), (tag: Tag) => tag.name)
                )
            )
        ));
    }

    public getEnvironments(): string {
        return StringUtilities.getCommaSeparatedList(
            _.map(this.agentHealth.registrationStatus.environments, e => e.toString()),
            {},
            useCmsHere_NoEnvironments
        );
    }

    public updateStatus(): void {
        let agentRegistrationStatus = this.agentHealth.registrationStatus;
        let allAssetGroupsValid = _.all(agentRegistrationStatus.assetGroups, ag => ag.isComplete);

        if (!allAssetGroupsValid) {
            return;
        }

        agentRegistrationStatus.assetGroupsStatus = RegistrationState.valid;

        if (agentRegistrationStatus.capabilityStatus === RegistrationState.valid &&
            agentRegistrationStatus.environmentStatus === RegistrationState.valid &&
            agentRegistrationStatus.protocolStatus === RegistrationState.valid
          ) {
            this.agentHealth.agentHealthIcon = HealthIcon.healthy;
            agentRegistrationStatus.isComplete = true;
          }
    }
}
