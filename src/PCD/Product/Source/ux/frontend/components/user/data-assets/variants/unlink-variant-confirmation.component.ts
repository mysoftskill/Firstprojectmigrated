import { Component, Inject } from "../../../../module/app.module";
import template = require("./unlink-variant-confirmation.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { ConfirmationModalData } from "../../../shared/utilities/confirmation-modal-actions.component";
import { IContactService } from "../../../../shared/contact.service";

export interface UnlinkVariantConfirmationModalData extends ConfirmationModalData {
    variant: Pdms.VariantDefinition;
}

@Component({
    name: "pcdUnlinkVariantConfirmation",
    options: {
        template
    }
})
@Inject("$meeModal", "contactService")
export default class UnlinkVariantConfirmationComponent implements ng.IComponentController {
    public variant: Pdms.VariantDefinition;

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly contactService: IContactService) { }

    public $onInit(): void {
        let data = this.$meeModal.getData<UnlinkVariantConfirmationModalData>();

        this.variant = data.variant;
    }

    public requestAdminAssistance(): void {
        this.contactService.requestAdminAssistance("move-team-assets", {
            entityId: this.variant.id
        });
    }
}
