import { Component, Inject } from "../../../module/app.module";
import template = require("./delete.html!text");

import * as ManualRequest from "./../../../shared/manual-requests/manual-request-types";
import { IAngularFailureResponse } from "../../../shared/ajax.service";
import { IManualRequestsDataService } from "../../../shared/manual-requests/manual-requests-data.service";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import PrivacySubjectSelector from "../../shared/privacy-subject/privacy-subject-selector.component";
import CountryListSelectorComponent from "../../shared/utilities/country-list-selector.component";
import { BreadcrumbNavigation } from "../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManualRequestsLandingPageBreadcrumb } from "../route-config";
import { Lazy } from "../../../shared/utilities/lazy";

const errorCategory = "manual-requests.delete";

const useCmsHere_PageHeading = "Delete request";

@Component({
    name: "pcdManualRequestsDelete",
    options: {
        template
    }
})
@Inject("$q", "manualRequestsDataService", "pcdErrorService", "$state", "correlationContext", "$meeComponentRegistry")
export default class ManualRequestsDelete implements ng.IComponentController, ManualRequest.PrivacySubjectSelectorParent {
    public errorCategory = errorCategory;
    public pageHeading: string = useCmsHere_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManualRequestsLandingPageBreadcrumb];

    public capId: string;
    public priority: ManualRequest.PrivacySubjectPriority = "Regular";
    public hasAcknowledgedUndoableAction: boolean;
    public dataTypesOnSubjectRequests: ManualRequest.DataTypesOnSubjectRequest;

    // Child controllers.
    public privacySubjectSelector: Lazy<PrivacySubjectSelector>;
    public countryListSelector: Lazy<CountryListSelectorComponent>;

    constructor(
        private $q: ng.IQService,
        private readonly manualRequestsDataService: IManualRequestsDataService,
        private readonly pcdError: IPcdErrorService,
        private readonly $state: ng.ui.IStateService,
        private readonly correlationContext: Bradbury.ICorrelationContextManager,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService
    ) {
        this.$meeComponentRegistry.register("PrivacySubjectSelectorParent", "ManualRequestsDelete", this);

        this.privacySubjectSelector = new Lazy<PrivacySubjectSelector>(() =>
            this.$meeComponentRegistry.getInstanceById<PrivacySubjectSelector>("PrivacySubjectSelector"));
        this.countryListSelector = new Lazy<CountryListSelectorComponent>(() => 
            this.$meeComponentRegistry.getInstanceById<CountryListSelectorComponent>("CountryListSelectorComponent"));
    }

    public $onInit(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        // Populate non-critical information.
        this.getDataTypesOnSubjectRequests();
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister("ManualRequestsDelete");
    }

    public getDataTypesOnSubjectRequests(): ng.IPromise<void> {
        return this.manualRequestsDataService.getDeleteDataTypesOnSubjectRequests()
            .then((response: ManualRequest.DataTypesOnSubjectRequest) => {
                this.dataTypesOnSubjectRequests = response;
            });
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("processDeleteRequest")
    public deleteClicked(): ng.IPromise<any> {
        if (!this.isValidationSuccessful()) {
            return this.$q.reject();
        }

        let identifierData: ManualRequest.PrivacySubjectIdentifier = this.privacySubjectSelector.getInstance().getIdentifierFormData();
        let countryOfResidence: string = this.countryListSelector.getInstance().getSelectedCountryIsoCode();

        //  Required for cross-service scenario tracking.
        this.correlationContext.setProperty("scenario-id", "ust.privacy.delete");

        return this.manualRequestsDataService.delete(identifierData, {
            capId: this.capId,
            countryOfResidence,
            priority: this.priority
        })
            .then((response: ManualRequest.OperationResponse) => {
                this.$state.go(".request-completed", {
                    capId: this.capId,
                    requestIds: response.ids
                }, { location: "replace" });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, ManualRequest.Manual_Request_Error_Override);
            })
            .finally(() => {
                this.correlationContext.deleteProperty("scenario-id");
            });
    }

    public resetErrors(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
        this.privacySubjectSelector.getInstance().resetErrors();
    }

    public privacySubjectChanged(): void {
        this.resetErrors();
        this.hasAcknowledgedUndoableAction = false;
    }

    private isValidationSuccessful(): boolean {
        this.resetErrors();
        let hasNoInputErrors = true;

        if (!this.capId) {
            this.pcdError.setRequiredFieldErrorForId(`${this.errorCategory}.cap-id`);
            hasNoInputErrors = false;
        }
        if (!this.privacySubjectSelector.getInstance().isValidationSuccessful()) {
            hasNoInputErrors = false;
        }

        return hasNoInputErrors;
    }
}
