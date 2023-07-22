import { Component, Inject } from "../../../module/app.module";
import template = require("./confirmation-modal-actions.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { IAngularFailureResponse } from "../../../shared/ajax.service";

export type ConfirmationModalReturnLocation = {
    stateId: string;
    stateParams?: any;
    stateOptions?: ng.ui.IStateOptions;
};

export interface ConfirmationModalData {
    //  An action to be executed, when user confirms the intent to execute an action.
    onConfirm: () => ng.IPromise<any>;

    //  A callback that checks whether the user is allowed to confirm the intent to execute an action. If not specified, user can always confirm.
    canConfirm?: () => boolean;

    //  State to switch to upon successful completion of the action (or cancelation, if returnOnCancelLocation is not specified). If not specified, "^" will be used.
    returnLocation?: string | ConfirmationModalReturnLocation;

    //  State to switch to, when user cancels the intent to execute an action. If not specified, returnLocation will be used.
    returnLocationOnCancel?: string | ConfirmationModalReturnLocation;
}

@Component({
    name: "pcdConfirmationModalActions",
    options: {
        template
    }
})
@Inject("pcdErrorService", "$meeModal")
export class ConfirmationModalActionsComponent implements ng.IComponentController {
    public readonly errorCategory = "confirmation-modal";
    private data: ConfirmationModalData;

    private readonly DefaultReturnLocation = "^";

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        this.data = this.$meeModal.getData<ConfirmationModalData>();
        this.pcdError.resetErrorsForCategory(this.errorCategory);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    public onConfirmClick(): ng.IPromise<any> {
        return this.data.onConfirm()
            .then(() => this.close())
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory);
            });
    }

    public onCancel(): void {
        this.close(this.data.returnLocationOnCancel);
    }

    public canConfirm(): boolean {
        return !this.data.canConfirm || this.data.canConfirm();
    }

    private close(returnLocationOverride?: string | ConfirmationModalReturnLocation): void {
        let modalHideOptions: MeePortal.OneUI.Angular.ModalStateHideOptions;
        let targetLocation = returnLocationOverride || this.data.returnLocation || this.DefaultReturnLocation;
        if (typeof targetLocation === "string") {
            modalHideOptions = {
                stateId: targetLocation
            };
        } else {
            modalHideOptions = {
                stateId: targetLocation.stateId,
                stateParams: targetLocation.stateParams,
                stateOptions: targetLocation.stateOptions
            };
        }

        this.$meeModal.hide(modalHideOptions);
    }
}
