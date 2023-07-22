import { Component, Inject } from "../../../../module/app.module";
import template = require("./link-variant.html!text");

import * as moment from "moment";
import { VariantLinkingContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { IPcdErrorService, PcdErrorOverrides } from "../../../../shared/pcd-error.service";
import { IVariantDataService } from "../../../../shared/variant/variant-data.service";
import { IAngularFailureResponse } from "../../../../shared/ajax.service";
import { RadioGroup, RadioOption } from "../../../../shared/shared-types";

const errorCategory = "manage-data-assets.link-variant";
const useCmsHere_FailedToLinkVariant = "Failed to link data assets to variant. Please try again.";
const useCmsHere_InvalidUriFormat = "Invalid URL. Please ensure it starts with https://.";
const useCmsHere_SignalFilteringDisabledLabel = "Disabled";
const useCmsHere_SignalFilteringEnabledLabel = "Enabled";
const useCmsHere_SignalFilteringDisabledDescription = "Signals will not be filtered from your data agent, however the variant will still be linked to your data assets.";
const useCmsHere_SignalFilteringEnabledDescription = " Signals will be filtered from your data agent in accordance with the variant capabilities.";

const Link_Variant_Error_Override: PcdErrorOverrides = {
    overrides: {
        genericErrorMessage: useCmsHere_FailedToLinkVariant,
        targetErrorIds: {
            tfsTrackingUris: "tfs-tracking-uri"
        },
        targetErrorMessages: {
            tfsTrackingUris: {
                invalidInput: useCmsHere_InvalidUriFormat
            }
        }
    },
    genericErrorId: "link-variant",
};

interface LinkVariantFormData {
    disabledSignalFiltering: boolean;
    tfsTrackingUri: string;
}

@Component({
    name: "pcdLinkVariant",
    options: {
        template
    }
})
@Inject("variantDataService", "$meeModal", "pcdErrorService")
export default class LinkVariantComponent implements ng.IComponentController {

    public context: VariantLinkingContext;
    public errorCategory = errorCategory;
    public formData: LinkVariantFormData = {
        disabledSignalFiltering: false,
        tfsTrackingUri: "",
    };
    public radioGroup: RadioGroup = {
        model: useCmsHere_SignalFilteringEnabledLabel,
        options: [
            {
                value: useCmsHere_SignalFilteringEnabledLabel,
                label: useCmsHere_SignalFilteringEnabledLabel,
                description: useCmsHere_SignalFilteringEnabledDescription
            },
            {
                value: useCmsHere_SignalFilteringDisabledLabel,
                label: useCmsHere_SignalFilteringDisabledLabel,
                description: useCmsHere_SignalFilteringDisabledDescription
            }
        ]
    };

    constructor(
        private readonly variantDataService: IVariantDataService,
        private readonly $modalState: MeePortal.OneUI.Angular.IModalStateService,
        private readonly pcdError: IPcdErrorService
    ) { }

    public $onInit() {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        this.context = this.$modalState.getData<VariantLinkingContext>();
    }

    public setSignalFiltering(): void {
        this.formData.disabledSignalFiltering = this.radioGroup.model === useCmsHere_SignalFilteringDisabledLabel;
    }

    public linkVariant(): void {

        if (this.hasDataEntryErrors()) {
            return;
        }

        this.performLinkVariant()
            .then(() => {
                this.context.onComplete();
                this.$modalState.hide("^");
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, Link_Variant_Error_Override);
            });
    }

    public hasDataEntryErrors(): boolean {

        let hasInputErrors = false;
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        return hasInputErrors;
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    private performLinkVariant(): ng.IPromise<void> {
        return this.variantDataService.createVariantRequest({
            id: null,
            ownerId: this.context.ownerId,
            ownerName: null,
            requestedVariants: _.map(this.context.variants, v => {
                return {
                    variantId: v.id,
                    variantName: null,
                    disabledSignalFiltering: this.formData.disabledSignalFiltering,
                    tfsTrackingUris: (this.formData.tfsTrackingUri ? [this.formData.tfsTrackingUri] : []),
                    variantState: null
                };
            }),
            variantRelationships: this.context.assetGroups.map(ag => {
                return {
                    assetGroupId: ag.id,
                    assetGroupQualifier: ag.qualifier
                };
            }),
            trackingDetails: null
        });
    }

    public cancel(): void {
        this.$modalState.hide("^");
    }
}
