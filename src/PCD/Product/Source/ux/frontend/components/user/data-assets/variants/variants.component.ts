import { Component, Inject } from "../../../../module/app.module";
import template = require("./variants.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { AssetGroupVariant } from "../../../../shared/variant/variant-types";
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageDataAssetsPageBreadcrumb, VariantPageBreadcrumb } from "../breadcrumbs-config";
import { IVariantDataService } from "../../../../shared/variant/variant-data.service";
import { UnlinkVariantConfirmationModalData } from "./unlink-variant-confirmation.component";
import { VariantsStateParams } from "../management-flows";

@Component({
    name: "pcdVariants",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$q", "$stateParams", "$meeModal", "variantDataService")
export default class VariantView implements ng.IComponentController {
    public pageHeading = VariantPageBreadcrumb.headingText;

    public assetGroup: Pdms.AssetGroup;
    public assetGroupId: string;
    public variantDefinitions: Pdms.VariantDefinition[] = [];
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAssetsPageBreadcrumb];

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $q: ng.IQService,
        private readonly $stateParams: VariantsStateParams,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly variantDataService: IVariantDataService) {
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeVariantViewComponent")
    public $onInit(): ng.IPromise<any> {
        this.assetGroupId = this.$stateParams.assetGroupId;
        return this.pdmsDataService.getAssetGroupById(this.assetGroupId)
            .then((assetGroup: Pdms.AssetGroup) => {
                this.assetGroup = assetGroup;

                let variantRequests: ng.IPromise<any>[] = _.map(assetGroup.variants, (v: AssetGroupVariant) =>
                    this.pdmsDataService.getVariantById(v.variantId)
                        .then((variantDefinition: Pdms.VariantDefinition) =>
                            this.variantDefinitions.push(variantDefinition)
                        )
                );

                return this.$q.all(variantRequests);
            });
    }

    public onUnlinkVariant(assetGroupId: string, variantId: string, eTag: string): ng.IPromise<any> {
        return this.variantDataService.unlinkVariant(assetGroupId, variantId, eTag)
            .then((assetGroup: Pdms.AssetGroup) => {
                this.assetGroup = assetGroup;
                this.variantDefinitions = _.select(this.variantDefinitions,
                    (variantDefinition: Pdms.VariantDefinition) => variantDefinition.id !== variantId);
            });
    }

    public showUnlinkVariantConfirmationDialog(variant: Pdms.VariantDefinition): void {
        let data: UnlinkVariantConfirmationModalData = {
            variant: variant,
            onConfirm: () => this.onUnlinkVariant(this.assetGroup.id, variant.id, this.assetGroup.eTag),
            // "^" redirect the modal to this page, "^.^" redirects to the parent (manage data assets) page.
            returnLocation: (this.variantDefinitions.length > 1) ? "^" : "^.^",
            returnLocationOnCancel: "^"
        };
        this.$meeModal.show("#modal-dialog", ".unlink", { data });
    }
}
