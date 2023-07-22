import { Component, Inject } from "../../../module/app.module";
import template = require("./privacy-subject-microsoft-employee-identifier.html!text");

import * as moment from "moment";
import * as ManualRequests from "../../../shared/manual-requests/manual-request-types";
import PrivacySubjectSelector from "./privacy-subject-selector.component";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { NamedResourceSelectorData, ResourceEntity } from "../directory-resource-selector/directory-resource-selector-types";
import { Lazy } from "../../../shared/utilities/lazy";

const defaultErrorCategory = "microsoft-employee-identifier";
const useCmsHere_InvalidDateFormat = "Invalid date format used. Please ensure it is in the form 'MM-DD-YYYY'.";

@Component({
    name: "pcdPrivacySubjectMicrosoftEmployeeIdentifier",
    options: {
        template,
        bindings: {
            errorCategory: "@?pcdErrorCategory"
        }
    }
})
@Inject("pcdErrorService", "$meeComponentRegistry")
export default class PrivacySubjectMicrosoftEmployeeIdentifier implements ManualRequests.PrivacySubjectIdentifierFormEntryComponent {
    public errorCategory: string;
    public identifierType = ManualRequests.PrivacySubjectDetailTypeId[ManualRequests.PrivacySubjectDetailTypeId.MicrosoftEmployee];

    public identifierFormData: ManualRequests.MicrosoftEmployeeSubject;

    public emailAccountsSelectorData: NamedResourceSelectorData = {
        resources: <ResourceEntity[]>[],
        isAutoSuggestAllowed: false,
        autoSuggestionList: <string[]>[]
    };

    public parentCtrl: Lazy<PrivacySubjectSelector>;

    public constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService
    ) {
        this.$meeComponentRegistry.register("PrivacySubject", this.identifierType, this);

        this.parentCtrl = new Lazy<PrivacySubjectSelector>(() =>
            this.$meeComponentRegistry.getInstanceById("PrivacySubjectSelector"));
    }

    public $onInit(): void {
        if (!this.errorCategory) {
            this.errorCategory = defaultErrorCategory;
        }

        this.resetForm();
        
        this.parentCtrl.getInstance().privacySubjectChanged();
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister(this.identifierType);
        
        this.parentCtrl.getInstance().privacySubjectChanged();
    }

    public hasDataEntryErrors(): boolean {
        let hasInputErrors = false;

        if (!this.emailAccountsSelectorData || !this.emailAccountsSelectorData.resources || !this.emailAccountsSelectorData.resources.length) {
            this.pcdError.setRequiredFieldErrorForId(`${this.errorCategory}.email-account`);
            hasInputErrors = true;
        }

        if (!this.identifierFormData.employeeId) {
            this.pcdError.setRequiredFieldErrorForId(`${this.errorCategory}.employee-id`);
            hasInputErrors = true;
        }

        if (!this.identifierFormData.employmentStartDate) {
            this.pcdError.setRequiredFieldErrorForId(`${this.errorCategory}.employment-start-date`);
            hasInputErrors = true;
        } else if (!moment(this.identifierFormData.employmentStartDate, "MM-DD-YYYY", /* strict */true).isValid()) {
            // Invalid date format entered
            this.pcdError.setErrorForId(`${this.errorCategory}.employment-start-date`, useCmsHere_InvalidDateFormat);
            hasInputErrors = true;
        }

        if (this.identifierFormData.employmentEndDate &&
                !moment(this.identifierFormData.employmentEndDate, "MM-DD-YYYY", /* strict */true).isValid()) {
            // Invalid date format entered
            this.pcdError.setErrorForId(`${this.errorCategory}.employment-end-date`, useCmsHere_InvalidDateFormat);
            hasInputErrors = true;
        }

        return hasInputErrors;
    }

    public resetErrors(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
    }

    public resetForm(): void {
        this.resetErrors();
        this.identifierFormData = {
            kind: "MicrosoftEmployee",
            emails: null,
            employeeId: null,
            employmentStartDate: null,
            employmentEndDate: null
        };
    }

    public getIdentifierFormData(): ManualRequests.MicrosoftEmployeeSubject {
        this.identifierFormData.emails = this.emailAccountsSelectorData ? _.map(this.emailAccountsSelectorData.resources,
            (r: ResourceEntity) => r.id) : [];
        return this.identifierFormData;
    }
}
