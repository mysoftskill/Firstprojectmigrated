import { Component, Inject } from "../../../../module/app.module";
import template = require("./view-data-agent-health.html!text");

const useCmsHere_PageHeading = "Agent health check";

interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Data agent ID. 
     **/
    agentId: string;
}

@Component({
    name: "pcdViewDataAgentHealth",
    options: {
        template
    }
})
@Inject("$stateParams")
export default class ViewDataAgentHealthComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;

    public agentId: string;

    constructor(
        private readonly $stateParams: StateParams) { }

    public $onInit(): void {
        this.agentId = this.$stateParams.agentId;
    }
}
