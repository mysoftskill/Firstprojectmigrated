import { Component, Inject } from "../../../../module/app.module";
import template = require("./view-data-agent.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";

const useCmsHere_PageHeading = "Data agent information";

interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Data agent ID. 
     **/
    agentId: string;
}

@Component({
    name: "pcdViewDataAgent",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$stateParams")
export default class ViewDataAgentComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;

    public dataAgent: Pdms.DataAgent;
    public owner: Pdms.DataOwner;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $stateParams: StateParams) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeViewDataAgentComponent")
    public $onInit(): ng.IPromise<any> {
        return this.pdmsDataService.getDeleteAgentById(this.$stateParams.agentId)
            .then(dataAgent => {
                this.dataAgent = dataAgent;
                return this.pdmsDataService.getDataOwnerWithServiceTree(dataAgent.ownerId)
                    .then(owner => {
                        this.owner = owner;
                    });
            });
    }
}
