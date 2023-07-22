import { Component, Inject } from "../../../../module/app.module";
import template = require("./create-data-asset.html!text");

import * as ManagementFlows from "../management-flows";
import * as Pdms from "../../../../shared/pdms/pdms-types";

const useCmsHere_PageHeading = "Register data asset";
const useCmsHere_NewAssetsListHeader = "Newly registered data assets";

@Component({
    name: "pcdCreateDataAsset",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$stateParams")
export default class CreateDataAssetComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;

    public currentAssetGroup: Pdms.AssetGroup;
    public newAssets: Pdms.AssetGroup[] = [];
    public newAssetsListHeader = useCmsHere_NewAssetsListHeader;
    public ownerId: string;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $stateParams: ManagementFlows.StateParams) { }

    public $onInit(): void {
        this.ownerId = this.$stateParams.ownerId;
        this.resetCurrentAssetGroup();
    }

    public saveAssetGroupQualifier(qualifier: Pdms.AssetGroupQualifier): ng.IPromise<any> {
        this.currentAssetGroup.qualifier = qualifier;

        return this.pdmsDataService.updateAssetGroup(this.currentAssetGroup).then((result: Pdms.AssetGroup) => {
            this.newAssets.push(result);
            this.resetCurrentAssetGroup();
        });
    }

    private resetCurrentAssetGroup(): void {
        this.currentAssetGroup = {
            id: "",
            ownerId: this.ownerId,
            qualifier: null,
        };
    }
}
