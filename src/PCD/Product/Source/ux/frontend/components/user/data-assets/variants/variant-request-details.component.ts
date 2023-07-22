import { Component, Inject } from "../../../../module/app.module";
import template = require("./variant-request-details.html!text");
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageDataAssetsPageBreadcrumb, VariantRequestsPageBreadcrumb } from "../breadcrumbs-config";

import * as Variant from "../../../../shared/variant/variant-types";
import { IVariantDataService } from "../../../../shared/variant/variant-data.service";

import { DeleteVariantRequestConfirmationModalData } from "./delete-variant-request-confirmation.component";

interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Variant request ID. 
     **/
    variantRequestId: string;
}

@Component({
    name: "pcdVariantRequestDetails",
    options: {
        template
    }
})
@Inject("$meeModal", "$stateParams", "$q", "variantDataService")
export default class VariantRequestDetailsComponent implements ng.IComponentController {
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAssetsPageBreadcrumb, VariantRequestsPageBreadcrumb];
    public requests: Variant.VariantRequest[];
    public request: Variant.VariantRequest;
    public variantRequestId: string;

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly $stateParams: StateParams,
        private readonly $q: ng.IQService,
        private readonly variantDataService: IVariantDataService) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeVariantRequestDetailsComponent")
    public $onInit(): ng.IPromise<any> {
        this.variantRequestId = this.$stateParams.variantRequestId;
        return this.$q.all([
            // TODO: Find a solution that doesn't require loading all the variant requests just to figure out where to route to.
            // Bug: 17099353
            this.variantDataService.getVariantRequestsByOwnerId(this.$stateParams.ownerId, this.$stateParams.assetGroupId)
                .then((variantRequests: Variant.VariantRequest[]) => {
                    this.requests = variantRequests;
                }),
            this.variantDataService.getVariantRequestById(this.variantRequestId)
                .then((variantRequest: Variant.VariantRequest) => {
                    this.request = variantRequest;
                })
        ]);
    }

    public showDeleteVariantRequestConfirmationDialog(): void {
        let data: DeleteVariantRequestConfirmationModalData = {
            variantRequest: this.request,
            onConfirm: () => this.onDeleteVariantRequest(),
            // "^.^" redirect the modal to the parent (variant requests) page,
            // "^.^.^" redirects to the parent-parent (manage data assets) page.
            returnLocation: (this.requests.length > 1) ? "^.^" : "^.^.^",
            returnLocationOnCancel: "^"
        };
        this.$meeModal.show("#modal-dialog", ".delete", { data });
    }

    private onDeleteVariantRequest(): ng.IPromise<any> {
        return this.variantDataService.deleteVariantRequestById(this.request.id);
    }
}
