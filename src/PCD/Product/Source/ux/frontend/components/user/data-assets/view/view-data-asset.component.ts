import { Component, Inject } from "../../../../module/app.module";
import template = require("./view-data-asset.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";

const useCmsHere_PageHeading = "Data asset information";

interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Asset group ID. 
     **/
    assetId: string;
}

@Component({
    name: "pcdViewDataAsset",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$stateParams")
export default class ViewDataAssetComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;

    public owner: Pdms.DataOwner;
    public assetGroup: Pdms.AssetGroup;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $stateParams: StateParams) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeViewAssetGroupComponent")
    public $onInit(): ng.IPromise<any> {
        return this.pdmsDataService.getAssetGroupById(this.$stateParams.assetId)
            .then(assetGroup => {
                this.assetGroup = assetGroup;
                if (assetGroup.ownerId) {
                    return this.pdmsDataService.getDataOwnerWithServiceTree(assetGroup.ownerId)
                        .then(owner => {
                            this.owner = owner;
                        });
                }
            });
    }
}
