import { Component, Inject } from "../../../module/app.module";
import template = require("./delete-asset-group-confirmation.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { ConfirmationModalData } from "../utilities/confirmation-modal-actions.component";
import { IContactService } from "../../../shared/contact.service";

export interface DeleteAssetGroupConfirmationModalData extends ConfirmationModalData {
    assetGroup: Pdms.AssetGroup;
}

@Component({
    name: "pcdDeleteAssetGroupConfirmation",
    options: {
        template
    }
})
@Inject("$meeModal", "contactService")
export default class DeleteAssetGroupConfirmationComponent implements ng.IComponentController {
    public assetGroup: Pdms.AssetGroup;
    public hasAcknowledgedDelete = false;

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly contactService: IContactService) { }

    public $onInit(): void {
        const data = this.$meeModal.getData<DeleteAssetGroupConfirmationModalData>();

        this.assetGroup = data.assetGroup;
        data.canConfirm = () => this.hasAcknowledgedDelete;
    }

    public requestAdminAssistance(): void {
        this.contactService.requestAdminAssistance("move-team-assets", {
            entityId: this.assetGroup.id
        });
    }
}
