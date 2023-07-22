import { Component, Inject } from "../../../../module/app.module";
import template = require("./variant-details.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { AssetGroupVariant } from "../../../../shared/variant/variant-types";
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageDataAssetsPageBreadcrumb, VariantPageBreadcrumb } from "../breadcrumbs-config";
import { IStringFormatFilter } from "../../../../shared/filters/string-format.filter";
import { VariantsStateParams } from "../management-flows";

interface VariantDetailsStateParams extends VariantsStateParams {
    variantId: string;
}

const useCmsHere_PageHeading = "Data asset variant: {0}";

@Component({
    name: "pcdVariantDetails",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$stateParams", "stringFormatFilter")
export default class VariantDetails implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAssetsPageBreadcrumb, VariantPageBreadcrumb];
    public assetGroupId: string;
    public variantId: string;

    public assetGroup: Pdms.AssetGroup;
    public variantDefinition: Pdms.VariantDefinition;
    public tfsTrackingUris: string[];
    public disabledSignalFiltering: boolean;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $stateParams: VariantDetailsStateParams,
        private readonly stringFormatFilter: IStringFormatFilter) {
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeVariantDetailsComponent")
    public $onInit(): ng.IPromise<any> {
        this.assetGroupId = this.$stateParams.assetGroupId;
        this.variantId = this.$stateParams.variantId;

        return this.pdmsDataService.getAssetGroupById(this.assetGroupId)
            .then((assetGroup: Pdms.AssetGroup) => {
                this.assetGroup = assetGroup;
                let variant: AssetGroupVariant = _.find(assetGroup.variants, (v: AssetGroupVariant) => v.variantId === this.variantId);

                this.tfsTrackingUris = variant.tfsTrackingUris;
                this.disabledSignalFiltering = variant.disabledSignalFiltering;

                return this.pdmsDataService.getVariantById(this.variantId).then((variantDefinition: Pdms.VariantDefinition) => {
                    this.variantDefinition = variantDefinition;
                    this.pageHeading = this.stringFormatFilter(useCmsHere_PageHeading, [variantDefinition.name]);
                });
            });
    }
}
