import { Component, Inject } from "../../../../module/app.module";
import template = require("./edit-data-asset.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageDataAssetsPageBreadcrumb } from "../breadcrumbs-config";
import { IAngularFailureResponse } from "../../../../shared/ajax.service";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";

const useCmsHere_PageHeading = "Edit data asset";

interface StateParams extends ng.ui.IStateParamsService {
    assetGroupId: string;
    ownerId: string;
}

@Component({
    name: "pcdEditDataAsset",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$state", "$stateParams", "$q", "pcdErrorService")
export default class EditDataAssetComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAssetsPageBreadcrumb];
    public errorCategory = "asset-editor";

    public owner: Pdms.DataOwner;
    public assetGroup: Pdms.AssetGroup;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $stateParams: StateParams,
        private readonly $q: ng.IQService,
        private readonly pcdError: IPcdErrorService) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeEditDataAssetComponent")
    public $onInit(): ng.IPromise<any> {

        return this.$q.all([
            this.pdmsDataService.getDataOwnerWithServiceTree(this.$stateParams.ownerId)
                .then((owner: Pdms.DataOwner) => {
                    this.owner = owner;
                }),
            this.pdmsDataService.getAssetGroupById(this.$stateParams.assetGroupId)
                .then((assetGroup: Pdms.AssetGroup) => {
                    this.assetGroup = assetGroup;
                })
        ]);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("assetEditorSave")
    private saveClicked(): ng.IPromise<any> {
        return this.pdmsDataService.updateAssetGroup(this.assetGroup)
            .then(assetGroup => {
                this.$state.go("^", { ownerId: this.owner.id });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, {
                    genericErrorId: "save"
                });
            });
    }
}
