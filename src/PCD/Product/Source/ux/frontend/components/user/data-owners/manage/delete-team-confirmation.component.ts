import { Component, Inject, Route } from "../../../../module/app.module";
import template = require("./delete-team-confirmation.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { ConfirmationModalData } from "../../../shared/utilities/confirmation-modal-actions.component";

export interface DeleteTeamConfirmationModalData extends ConfirmationModalData {
    owner: Pdms.DataOwner;
}

@Component({
    name: "pcdDeleteTeamConfirmation",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$meeModal")
export default class DeleteTeamConfirmationComponent implements ng.IComponentController {
    public owner: Pdms.DataOwner;

    public hasAcknowledgedDelete = false;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        const data = this.$meeModal.getData<DeleteTeamConfirmationModalData>();

        this.owner = data.owner;
        data.canConfirm = () => this.hasAcknowledgedDelete;
    }
}
