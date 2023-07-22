import { Component, Inject } from "../../../module/app.module";
import template = require("./asset-group-qualifier-view.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

@Component({
    name: "pcdAssetGroupQualifierView",
    options: {
        template,
        bindings: {
            qualifier: "<pcdQualifier",
            assetGroupId: "<?pcdAssetGroupId",
            hideDataGridLink: "<?pcdHideDataGridLink"
        }
    }
})
@Inject("$state")
export default class AssetGroupQualifierView implements ng.IComponentController {
    // Inputs.
    public qualifier: Pdms.AssetGroupQualifier;
    public hideDataGridLink = false;
    public assetGroupId: string;

    constructor(
        private readonly $state: ng.ui.IStateService) {
    }

    public shouldHideDataGridLink(): boolean {
        return this.hideDataGridLink || !this.hasDataGridLink();
    }

    public getDataGridLink(): string {
        if (!this.hasDataGridLink()) {
            return "";
        }

        //  Make DataGrid search across all tenants, without using DataGrid Next.
        return `${this.qualifier.dataGridLink}&teamPath=0`;
    }

    public shouldHideViewLink(): boolean {
        return !this.assetGroupId;
    }

    public getViewLink(): string {
        return this.$state.href("data-assets.view", { assetId: this.assetGroupId }, { absolute: true });
    }

    private hasDataGridLink(): boolean {
        return !!this.qualifier && !!this.qualifier.dataGridLink;
    }
}
