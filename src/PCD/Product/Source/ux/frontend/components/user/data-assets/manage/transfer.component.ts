import { Component, Inject } from "../../../../module/app.module";
import template = require("./transfer.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { TransferAssetGroupContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";
import { IAngularFailureResponse } from "../../../../shared/ajax.service";

export interface LinkUnlinkStrings {
    buttonLabel: string;
    title: string;
    actionsLabel: string;
    selectedDataAgentLabel: string;
}

const errorCategory = "manage-data-assets.transfer";

@Component({
    name: "pcdTransferAssetGroup",
    options: {
        template,
        bindings: {
        }
    }
})
@Inject("pdmsDataService", "$meeModal", "pcdErrorService")
export default class TransferAssetGroupComponent implements ng.IComponentController {
    public context: TransferAssetGroupContext;
    public errorCategory = errorCategory;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $modalState: MeePortal.OneUI.Angular.IModalStateService,
        private readonly pcdError: IPcdErrorService
    ) { }

    public $onInit() {
        this.context = this.$modalState.getData<TransferAssetGroupContext>();
        
        this.pcdError.resetErrorsForCategory(this.errorCategory);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    public transfer(): ng.IPromise<any> {
        this.pcdError.resetErrorForId(`${this.errorCategory}.transfer-to-owner`);

        return this.pdmsDataService.createTransferRequest({
                id: null,
                assetGroups: this.context.assetGroups,
                requestState: this.context.requestState,
                sourceOwnerId: this.context.sourceOwnerId,
                sourceOwnerName: null,
                targetOwnerId: this.context.targetOwnerId
            })
            .then(() => {
                this.context.onComplete();
                this.$modalState.hide("^");
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, { genericErrorId: "transfer-to-owner" });
            });
    }

    public cancel(): void {
        this.$modalState.hide("^");
    }
}
