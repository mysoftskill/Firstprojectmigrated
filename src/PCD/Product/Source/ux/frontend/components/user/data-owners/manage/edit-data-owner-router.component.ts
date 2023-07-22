import { Component, Inject, Route } from "../../../../module/app.module";
import template = require("./edit-data-owner-router.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";

const useCmsHere_PageHeading = "Edit team";

/** 
 * State parameters. 
 **/
export interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Data owner ID. 
     **/
    ownerId: string;
}

@Component({
    name: "pcdEditDataOwnerRouter",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$state", "$stateParams")
export default class EditDataOwnerRouterComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $stateParams: StateParams) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeEditDataOwnerRouterComponent")
    public $onInit(): ng.IPromise<any> {
        return this.pdmsDataService.getDataOwnerWithServiceTree(this.$stateParams.ownerId)
            .then((owner: Pdms.DataOwner) => {
                if (owner.serviceTree && owner.serviceTree.id) {
                    this.$state.go(".service-tree", { dataOwner: owner });
                } else {
                    this.$state.go(".pdms", { dataOwner: owner });
                }
            });
    }
}
