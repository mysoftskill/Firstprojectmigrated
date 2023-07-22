import { Component, Inject } from "../../../../module/app.module";
import template = require("./approve-asset-transfer-request-confirmation.html!text");
import { AssetTransferRequestsConfirmationModalData, RequestContainer } from "../management-flows";


@Component({
    name: "pcdApproveAssetTransferRequestConfirmation",
    options: {
        template
    }
})
@Inject("$meeModal")
export default class ApproveAssetTransferRequestConfirmationComponent implements ng.IComponentController {
    public requestContainers: RequestContainer[];

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        let data = this.$meeModal.getData<AssetTransferRequestsConfirmationModalData>();
        this.requestContainers = data.requests;
    }
}
