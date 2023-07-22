import { Component, Inject } from "../../../../module/app.module";
import template = require("./agent-operational-readiness.html!text");

import { IPcdErrorService } from "../../../../shared/pcd-error.service";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import { OperationalReadiness } from "../../../../shared/pdms/pdms-types";
import { IAngularFailureResponse } from "../../../../shared/ajax.service";

const useCmsHere_PageHeading = "Agent operational readiness";

@Component({
    name: "pcdAgentOperationalReadiness",
    options: {
        template
    }
})
@Inject("pcdErrorService", "$stateParams", "pdmsDataService", "$q", "$state")
export default class AgentOperationalReadiness implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
    public operationalReadiness: OperationalReadiness;
    public owner: Pdms.DataOwner;
    public agent: Pdms.DataAgent;

    public readonly errorCategory = "operational-readiness";

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly $stateParams: ng.ui.IStateParamsService,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $q: ng.IQService,
        private readonly $state: ng.ui.IStateService) {
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchAgentOperationalReadiness")
    public $onInit(): ng.IPromise<any> {
        return this.$q.all([
            this.pdmsDataService.getDeleteAgentById(this.$stateParams.agentId)
                .then((agent: Pdms.DeleteAgent) => {
                    this.agent = agent;
                    this.operationalReadiness = agent.operationalReadiness;
                }),
            this.pdmsDataService.getDataOwnerWithServiceTree(this.$stateParams.ownerId)
                .then((owner: Pdms.DataOwner) => {
                    this.owner = owner;
                })
        ]);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("saveAgent")
    private saveAgent(): ng.IPromise<any> {
        return this.pdmsDataService.updateDataAgent(this.agent)
            .then(data => {
                this.$state.go("^", { ownerId: this.owner.id });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory);
            });
    }
}
