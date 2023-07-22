import { Component, Inject } from "../../../../module/app.module";
import template = require("./manage-data-assets.html!text");
import { StateParams } from "../management-flows";
import { ManageDataAssetsPageBreadcrumb } from "../breadcrumbs-config";

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { DeleteAssetGroupConfirmationModalData } from "../../../shared/asset-group/delete-asset-group-confirmation.component";

@Component({
    name: "pcdManageDataAssets",
    options: {
        template
    }
})
@Inject("$stateParams", "pdmsDataService", "$meeModal")
export default class ManageDataAssetsComponent implements ng.IComponentController {
    public pageHeading = ManageDataAssetsPageBreadcrumb.headingText;

    public assetGroups: Pdms.AssetGroup[];
    public ownerId: string;

    constructor(
        private readonly $stateParams: StateParams,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) {
    }

    public $onInit(): ng.IPromise<void> {
        this.ownerId = this.$stateParams.ownerId;
        return this.getDataAssetsForOwner();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchManageDataAssetsForOwner")
    public getDataAssetsForOwner(): ng.IPromise<any> {
        return this.pdmsDataService.getAssetGroupsByOwnerId(this.ownerId)
            .then((assetGroups: Pdms.AssetGroup[]) => {
                this.assetGroups = assetGroups;
            });
    }

    public hasPendingTransferRequests(): boolean {
        return _.any(this.assetGroups, (assetGroup:Pdms.AssetGroup) => assetGroup.hasPendingTransferRequest);
    }

    public onDeleteAsset(assetGroup: Pdms.AssetGroup): ng.IPromise<any> {
        // TODO: Bug 16069874 notify user when delete fails
        return this.pdmsDataService.deleteAssetGroup(assetGroup).then(() => {
            this.assetGroups.splice(this.assetGroups.indexOf(assetGroup), 1);
        });
    }

    public showDeleteAssetConfirmationDialog(assetGroup: Pdms.AssetGroup): void {
        let data: DeleteAssetGroupConfirmationModalData = {
            assetGroup,
            onConfirm: () => this.onDeleteAsset(assetGroup)
        };
        this.$meeModal.show("#modal-dialog", ".delete-asset-group", { data });
    }
}
