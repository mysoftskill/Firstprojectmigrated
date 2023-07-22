import { Component, Inject } from "../../../module/app.module";
import template = require("./owner-view-modal-trigger.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

export interface OwnerViewModalTriggerData {
    ownerId: string;
}

@Component({
    name: "pcdOwnerViewModalTrigger",
    options: {
        template,
    }
})
@Inject("$meeModal", "pdmsDataService")
export default class OwnerViewModalTriggerComponent implements ng.IComponentController {
    //  Input
    public ownerId: string;

    public owner: Pdms.DataOwner;

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly pdmsDataService: Pdms.IPdmsDataService) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    public $onInit(): ng.IPromise<any> {
        this.ownerId = this.$meeModal.getData<OwnerViewModalTriggerData>().ownerId;
        return this.pdmsDataService.getDataOwnerWithServiceTree(this.ownerId)
            .then((owner: Pdms.DataOwner) => {
                this.owner = owner;
            });
    }
}
