import { Component, Inject } from "../../../module/app.module";
import template = require("./request-details.html!text");
import { VariantRequest } from "../../../shared/variant/variant-types";
import { IVariantAdminDataService } from "../../../shared/variant-admin/variant-admin-data.service";
import { IVariantDataService } from "../../../shared/variant/variant-data.service";
import { IAngularFailureResponse } from "../../../shared/ajax.service";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { BreadcrumbNavigation } from "../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageVariantRequestsPageBreadcrumb } from "../route-config";

interface VariantRequestDetailsStateParams extends ng.ui.IStateParamsService {
    variantRequestId: string;
    request: VariantRequest;
}

const useCmsHere_PageHeading = "Variant request details";

@Component({
    name: "pcdAdminVariantRequestDetails",
    options: {
        template
    }
})
@Inject("pcdErrorService", "variantAdminDataService", "variantDataService", "$q", "$state", "$stateParams")
export default class AdminVariantRequestDetailsComponent implements ng.IComponentController {
    public request: VariantRequest;

    public errorCategory = "admin-variant-request-details-component";

    public pageHeading = useCmsHere_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManageVariantRequestsPageBreadcrumb];

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly variantAdminDataService: IVariantAdminDataService,
        private readonly variantDataService: IVariantDataService,
        private readonly $q: ng.IQService,
        private readonly $state: ng.ui.IStateService,
        private readonly $stateParams: VariantRequestDetailsStateParams
    ) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeAdminVariantRequestDetailsComponent")
    public $onInit(): ng.IPromise<any> {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        if (this.$stateParams.request) {
            this.request = this.$stateParams.request;
            return this.$q.resolve();
        }

        return this.variantDataService.getVariantRequestById(this.$stateParams.variantRequestId)
            .then((variantRequest: VariantRequest) => {
                this.request = variantRequest;
            });
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("processVariantRequest")
    public approveVariantRequest(): ng.IPromise<void> {
        return this.variantAdminDataService.approveVariantRequest(this.request.id)
            .then(() => {
                this.$state.go("^", {}, { location: "replace", reload: true });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory);
            });
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("processVariantRequest")
    public denyVariantRequest(): ng.IPromise<void> {
        return this.variantAdminDataService.denyVariantRequest(this.request.id)
            .then(() => {
                this.$state.go("^", {}, { location: "replace", reload: true });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory);
            });
    }
}
