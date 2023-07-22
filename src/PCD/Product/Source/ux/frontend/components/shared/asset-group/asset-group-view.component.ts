import { Component, Inject } from "../../../module/app.module";
import template = require("./asset-group-view.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { StringUtilities } from "../../../shared/string-utilities";

const useCmsHere_PendingVariantRequestsLabel = "There are variant requests pending";
const useCmsHere_NoPendingVariantRequestsLabel = "None";
const useCmsHere_NoOptionalFeatures = "None";

@Component({
    name: "pcdAssetGroupView",
    options: {
        template,
        bindings: {
            assetGroup: "<pcdAssetGroup",
            hideTitle: "<?pcdHideTitle"
        }
    }
})
@Inject("$state")
class AssetGroupViewComponent implements ng.IComponentController {
    // Input
    public assetGroup: Pdms.AssetGroup;
    public hideTitle: boolean;

    constructor(
        private readonly $state: ng.ui.IStateService) {
    }

    public getPendingVariantRequestDisplay(): string {
        return this.assetGroup.hasPendingVariantRequests ? useCmsHere_PendingVariantRequestsLabel : useCmsHere_NoPendingVariantRequestsLabel;
    }

    public getOptionalFeatures(): string {
        return StringUtilities.getCommaSeparatedList(this.assetGroup.optionalFeatures, {}, useCmsHere_NoOptionalFeatures);
    }

    public getViewLink(): string {
        return this.$state.href("data-assets.view", { assetId: this.assetGroup.id }, { absolute: true });
    }
}
