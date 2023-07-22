import { Component, Inject } from "../../../module/app.module";
import template = require("./agent-icm-confirmation.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { IAngularFailureResponse } from "../../../shared/ajax.service";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { AgentIcmConfirmationModalData } from "./route-config";

@Component({
    name: "pcdAgentIcmConfirmation",
    options: {
        template
    }
})
@Inject("pcdErrorService", "$meeModal")
export default class AgentIcmConfirmationComponent implements ng.IComponentController {
    public readonly errorCategory = "icm-confirmation-modal";
    private data: AgentIcmConfirmationModalData;
    public owner: Pdms.DataOwner;

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
        this.data = this.$meeModal.getData<AgentIcmConfirmationModalData>();
        
        this.owner = this.data.owner;
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    public onConfirmClick(): ng.IPromise<any> {
        return this.data.onConfirm()
            .then((data: Pdms.Incident) => {
                this.data.incident = data;
                this.$meeModal.switchTo("^.icm-response");
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory);
            });
    }

    public close(): void {
        this.$meeModal.hide("^");
    }
}
