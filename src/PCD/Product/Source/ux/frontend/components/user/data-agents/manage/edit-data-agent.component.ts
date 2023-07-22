import { Component, Inject } from "../../../../module/app.module";
import template = require("./edit-data-agent.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageDataAgentsPageBreadcrumb } from "../breadcrumbs-config";

const useCmsHere_PageHeading = "Edit data agent";

interface StateParams extends ng.ui.IStateParamsService {
    /**
     *  Data agent ID. 
     **/
    agentId: string;

    /**
     *  Owner ID. 
     **/
    ownerId: string;
}

@Component({
    name: "pcdEditDataAgent",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$state", "$stateParams", "$q")
export default class EditDataAgentComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAgentsPageBreadcrumb];

    public model: {
        owner: Pdms.DataOwner;
        dataAgent: Pdms.DataAgent;
    };

    public modalReturnLocation = "data-agents.manage.edit";

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $stateParams: StateParams,
        private readonly $q: ng.IQService) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeEditDataAgentComponent")
    public $onInit(): ng.IPromise<any> {
        this.model = {
            owner: null,
            dataAgent: null
        };

        return this.$q.all([
            this.pdmsDataService.getDataOwnerWithServiceTree(this.$stateParams.ownerId)
                .then((owner: Pdms.DataOwner) => {
                    this.model.owner = owner;
                }),
            this.pdmsDataService.getDeleteAgentById(this.$stateParams.agentId)
                .then((dataAgent: Pdms.DataAgent) => {
                    this.model.dataAgent = dataAgent;
                })
        ]);
    }

    public onDataAgentUpdated(updatedAgent: Pdms.DataAgent): void {
        this.$state.go("^", { ownerId: this.model.owner.id });
    }
}
