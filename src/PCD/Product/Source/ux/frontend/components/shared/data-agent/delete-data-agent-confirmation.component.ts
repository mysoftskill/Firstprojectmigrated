import { Component, Inject } from "../../../module/app.module";
import template = require("./delete-data-agent-confirmation.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { ConfirmationModalData } from "../utilities/confirmation-modal-actions.component";

export interface DeleteDataAgentConfirmationModalData extends ConfirmationModalData {
    dataAgent: Pdms.DataAgent;
}

@Component({
    name: "pcdDeleteDataAgentConfirmation",
    options: {
        template
    }
})
@Inject("$meeModal")
export default class DeleteDataAgentConfirmationComponent implements ng.IComponentController {
    public dataAgent: Pdms.DataAgent;
    public hasAcknowledgedDelete = false;

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        let data = this.$meeModal.getData<DeleteDataAgentConfirmationModalData>();
        this.dataAgent = data && data.dataAgent;
        data.canConfirm = () => this.checkIfErrorMessageShown() ? this.hasAcknowledgedDelete : true;
    }

    public checkIfErrorMessageShown(): boolean {
        if (this.dataAgent.pendingCommandsFound) {
            return true;
        }
    }
}
