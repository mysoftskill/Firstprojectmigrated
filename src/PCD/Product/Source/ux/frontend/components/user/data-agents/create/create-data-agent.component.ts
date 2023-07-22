import { Component, Inject } from "../../../../module/app.module";
import template = require("./create-data-agent.html!text");

import * as ManagementFlows from "../management-flows";
import * as Pdms from "../../../../shared/pdms/pdms-types";

const useCmsHere_PageHeading = "Register a data agent";

@Component({
    name: "pcdCreateDataAgent",
    options: {
        template,
        bindings: {
            kind: "@"
        }
    }
})
@Inject("pdmsDataService", "$state", "$stateParams")
export default class CreateDataAgentComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;

    /**
     *  Input: kind of data agent to create. 
     **/
    public kind: Pdms.DataAgentKind;

    public owner: Pdms.DataOwner;

    public dataAgent: Pdms.DataAgent;

    public modalReturnLocation = "data-agents.create";

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $stateParams: ManagementFlows.StateParams) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeCreateDataAgentComponent")
    public $onInit(): ng.IPromise<any> {
        return this.pdmsDataService.getDataOwnerWithServiceTree(this.$stateParams.ownerId).then(data => {
            this.owner = data;

            //  Create new data agent instance.
            let dataAgent = this.pdmsDataService.createNewDataAgentInstance(this.kind);
            dataAgent.ownerId = this.owner.id;
            this.dataAgent = dataAgent;
        });
    }

    public onDataAgentCreated(updatedAgent: Pdms.DataAgent): void {
        this.$state.go("^.manage", { ownerId: this.owner.id }, { location: "replace" });
    }
}
