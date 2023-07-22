import { Component, Inject } from "../../../../module/app.module";
import template = require("./view-data-owner.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";

const useCmsHere_PageHeading = "Data owner information";

interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Owner ID. 
     **/
    owner: string;
}

@Component({
    name: "pcdViewDataOwner",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$stateParams")
export default class ViewDataOwnerComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;

    public owner: Pdms.DataOwner;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $stateParams: StateParams) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeViewOwnerComponent")
    public $onInit(): ng.IPromise<any> {
        return this.pdmsDataService.getDataOwnerWithServiceTree(this.$stateParams.ownerId)
            .then(owner => {
                this.owner = owner;
            });
    }
}
