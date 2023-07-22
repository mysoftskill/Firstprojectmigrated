import { Component, Inject } from "../../../../module/app.module";
import template = require("./edit-privacy-actions.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";

import { DataOwnershipIndicator } from "../../../shared/storage-picker/asset-owner-indicator.component";
import { IPcdErrorService, PcdErrorOverrides } from "../../../../shared/pcd-error.service";
import { IAngularFailureResponse } from "../../../../shared/ajax.service";

interface EditPrivacyActionsData {
    dataAgentId: string;
    isDataOwner: DataOwnershipIndicator;
    assetGroup: Pdms.AssetGroup;
    privacyActionState: Pdms.PrivacyActionsState;
    protocol: string;
    onUpdate: (assetGroup: Pdms.AssetGroup) => ng.IPromise<void>;
}

@Component({
    name: "pcdEditPrivacyActions",
    options: {
        template
    }
})
@Inject("pcdErrorService", "$meeModal")
export default class EditPrivacyActionsComponent implements ng.IComponentController {
    public model: {
        dataAgentId: string,
        isDataOwner: DataOwnershipIndicator,
        assetGroup: Pdms.AssetGroup,
        privacyActionState: Pdms.PrivacyActionsState,
        protocol: string,
        onUpdate: (assetGroup: Pdms.AssetGroup) => ng.IPromise<void>,
    };
    public errorCategory = "edit-privacy-actions-component";

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
        this.model = this.$meeModal.getData<EditPrivacyActionsData>();
    }

    private getDeleteAgentState(): boolean {
        return !!this.model.assetGroup.deleteAgentId;
    }

    private getExportAgentState(): boolean {
        return !!this.model.assetGroup.exportAgentId;
    }

    public hasNoChangesInPrivacyActions(): boolean {
        return (this.getDeleteAgentState() === this.model.privacyActionState.deleteAction) &&
            (this.getExportAgentState() === this.model.privacyActionState.exportAction);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    public onSave(): ng.IPromise<any> {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
        let assetGroup = this.model.assetGroup;

        if (this.getDeleteAgentState() !== this.model.privacyActionState.deleteAction) {
            assetGroup.deleteAgentId = this.model.privacyActionState.deleteAction ? this.model.dataAgentId : null;
        }
        if (this.getExportAgentState() !== this.model.privacyActionState.exportAction) {
            assetGroup.exportAgentId = this.model.privacyActionState.exportAction ? this.model.dataAgentId : null;
        }

        return this.model.onUpdate(assetGroup)
            .then(() => this.onCancel())
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory);
            });
    }

    public onCancel(): void {
        this.$meeModal.hide({ stateId: "data-agents.manage.edit-scope" });
    }
}
