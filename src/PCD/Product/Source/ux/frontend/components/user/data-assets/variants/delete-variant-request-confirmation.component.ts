import { Component, Inject } from "../../../../module/app.module";
import template = require("./delete-variant-request-confirmation.html!text");

import * as Variant from "../../../../shared/variant/variant-types";
import { ConfirmationModalData } from "../../../shared/utilities/confirmation-modal-actions.component";

export interface DeleteVariantRequestConfirmationModalData extends ConfirmationModalData {
    variantRequest: Variant.VariantRequest;
}

@Component({
    name: "pcdDeleteVariantRequestConfirmation",
    options: {
        template
    }
})
@Inject("$meeModal")
export class DeleteVariantRequestConfirmationComponent implements ng.IComponentController {
    public variantRequest: Variant.VariantRequest;

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        let data = this.$meeModal.getData<DeleteVariantRequestConfirmationModalData>();
        this.variantRequest = data.variantRequest;
    }
}