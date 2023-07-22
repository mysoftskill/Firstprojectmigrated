import { Component, Inject } from "../../../../module/app.module";
import template = require("./view-agents-health.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { IRegistrationStatusDataService } from "../../../../shared/registration-status/registration-status-data.service";
import { AgentRegistrationStatus, HealthIcon, DataAgentWithHealthStatus }
    from "../../../../shared/registration-status/registration-status-types";

export type HealthFilterType = "all" | "issues";

@Component({
    name: "pcdViewAgentsHealth",
    options: {
        bindings: {
            agentId: "<pcdAgentId"
        },
        template
    }
})
@Inject("pdmsDataService", "registrationStatusDataService", "$q")
export default class ViewAgentsHealthComponent implements ng.IComponentController {
    //  Input
    public agentId: string;

    public agentHealth: DataAgentWithHealthStatus = {
        agent: null,
        owner: null,
        agentHealthIcon: null,
        registrationStatus: null
    };

    public displayByStatus: HealthFilterType = "all";

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly registrationStatusDataService: IRegistrationStatusDataService,
        private readonly $q: ng.IQService) {
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchAgentHealth")
    public $onInit(): ng.IPromise<any> {
        return this.$q.all([
            this.loadAgent(),
            this.loadStatusForAgent()
        ]);
    }

    private loadAgent(): ng.IPromise<any> {
        return this.pdmsDataService.getDeleteAgentById(this.agentId)
            .then((agent: Pdms.DeleteAgent) => {
                this.agentHealth.agent = agent;
                return this.pdmsDataService.getDataOwnerWithServiceTree(agent.ownerId)
                    .then((owner: Pdms.DataOwner) => {
                        this.agentHealth.owner = owner;
                    });
            });
    }

    public isAgentUnhealthy(): boolean {
        return this.agentHealth.registrationStatus && !this.agentHealth.registrationStatus.isComplete;
    }

    private loadStatusForAgent(): ng.IPromise<any> {
        return this.registrationStatusDataService.getAgentStatus(this.agentId)
            .then((registrationStatus: AgentRegistrationStatus) => {
                this.agentHealth.agentHealthIcon = registrationStatus.isComplete ? HealthIcon.healthy : HealthIcon.unhealthy;
                this.agentHealth.registrationStatus = registrationStatus;
            })
            .catch(() => {
                this.agentHealth.agentHealthIcon = HealthIcon.error;
            });
    }

    public isFilterByAll(): boolean {
        return this.displayByStatus === "all";
    }

    public isFilterByIssues(): boolean {
        return this.displayByStatus === "issues";
    }

    public filterViewHealth(filterValue: HealthFilterType): void {
        this.displayByStatus = filterValue;
    }
}
