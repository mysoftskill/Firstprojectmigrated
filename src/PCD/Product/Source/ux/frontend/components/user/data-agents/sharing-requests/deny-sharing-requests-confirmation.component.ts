import { Component, Inject } from "../../../../module/app.module";
import template = require("./deny-sharing-requests-confirmation.html!text");
import { SharingRequestsConfirmationModalData, RequestContainer } from "../management-flows";

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { ConfirmationModalData } from "../../../shared/utilities/confirmation-modal-actions.component";

@Component({
    name: "pcdDenySharingRequestConfirmation",
    options: {
        template
    }
})
@Inject("$meeModal")
export default class DenySharingRequestsConfirmationComponent implements ng.IComponentController {
    public requestContainers: RequestContainer[];

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        let data = this.$meeModal.getData<SharingRequestsConfirmationModalData>();
        this.requestContainers = data.requests;
    }
}
