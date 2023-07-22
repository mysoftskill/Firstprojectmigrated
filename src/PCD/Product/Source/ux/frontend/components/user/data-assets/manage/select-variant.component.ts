import { Component, Inject } from "../../../../module/app.module";
import template = require("./select-variant.html!text");

import { StateParams } from "../management-flows";
import { VariantLinkingContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";
import { IVariantDataService } from "../../../../shared/variant/variant-data.service";
import { VariantSelectorData } from "../../../shared/directory-resource-selector/directory-resource-selector-types";

const errorCategory = "manage-data-assets.select-variant";

@Component({
    name: "pcdSelectVariant",
    options: {
        template
    }
})
@Inject("variantDataService", "$state", "$q", "$stateParams", "$meeModal", "pcdErrorService")
export default class SelectVariant implements ng.IComponentController {

    public context: VariantLinkingContext;
    public errorCategory = errorCategory;
    public selectedVariantName = "";
    public variantSelectorData: VariantSelectorData = {
        variants: [],
    };

    constructor(
        private readonly variantDataService: IVariantDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $q: ng.IQService,
        private readonly $stateParams: StateParams,
        private readonly $modalState: MeePortal.OneUI.Angular.IModalStateService,
        private readonly pcdError: IPcdErrorService
        ) { }

    public $onInit() {
        this.context = this.$modalState.getData<VariantLinkingContext>();
    }

    public cancel(): void {
        this.$modalState.hide("^");
    }

    public noVariantsSelected(): boolean {
        return _.isEmpty(this.variantSelectorData.variants);
    }

    public next(): void {
        this.context.variants = this.variantSelectorData.variants;
        this.$modalState.switchTo("^.link-variant");
    }
}
