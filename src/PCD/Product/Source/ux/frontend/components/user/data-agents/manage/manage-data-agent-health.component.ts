import { Component, Inject } from "../../../../module/app.module";
import template = require("./manage-data-agent-health.html!text");
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageDataAgentsPageBreadcrumb } from "../breadcrumbs-config";

const useCmsHere_PageHeading = "Agent health check";

interface StateParams extends ng.ui.IStateParamsService {
    /**
     *  Data agent ID. 
     **/
    agentId: string;
}

@Component({
    name: "pcdManageDataAgentHealth",
    options: {
        template
    }
})
@Inject("$stateParams")
export default class ManageDataAgentHealthComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAgentsPageBreadcrumb];

    public agentId: string;

    constructor(
        private readonly $stateParams: StateParams) { }

    public $onInit(): void {
        this.agentId = this.$stateParams.agentId;
    }
}
