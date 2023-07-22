import { Component, Inject } from "../../../module/app.module";
import template = require("./privacy-subject-demographic-identifier.html!text");

import * as ManualRequests from "../../../shared/manual-requests/manual-request-types";
import PrivacySubjectSelector from "./privacy-subject-selector.component";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { NamedResourceSelectorData, ResourceEntity } from "../directory-resource-selector/directory-resource-selector-types";
import { Lazy } from "../../../shared/utilities/lazy";
import { IGroundControlApiService } from "../../../shared/flighting/ground-control-api.service";

const PRCFeatureFlag = "PCDPXS.PRCMakeEmailMandatory";

const defaultErrorCategory = "demographic-identifier";

function createNamedResourceSelectorModel(): NamedResourceSelectorData {
    return {
        resources: <ResourceEntity[]>[],
        isAutoSuggestAllowed: false,
        autoSuggestionList: <string[]>[]
    };
}

@Component({
    name: "pcdPrivacySubjectDemographicIdentifier",
    options: {
        template,
        bindings: {
            errorCategory: "@?pcdErrorCategory"
        }
    }
})
@Inject("pcdErrorService", "$meeComponentRegistry", "groundControlApiService")
export default class PrivacySubjectDemographicIdentifier implements ManualRequests.PrivacySubjectIdentifierFormEntryComponent {
    private isPRCFlagEnabled = false;
    
    private groundControlApiService: IGroundControlApiService;

    public errorCategory: string;
    
    public identifierType = ManualRequests.PrivacySubjectDetailTypeId[ManualRequests.PrivacySubjectDetailTypeId.Demographic];

    public identifierFormData: ManualRequests.DemographicSubject;

    public namesSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public emailsSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public phoneNumbersSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public unitNumbersSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public streetNumbersSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public streetNamesSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public citiesSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public regionsSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public postalCodesSelectorData: NamedResourceSelectorData = createNamedResourceSelectorModel();

    public parentCtrl: Lazy<PrivacySubjectSelector>;

    public constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService,
        private readonly groundControlApiServiceParam: IGroundControlApiService
    ) {
        this.$meeComponentRegistry.register("PrivacySubject", this.identifierType, this);
        this.parentCtrl = new Lazy<PrivacySubjectSelector>(() =>
        this.$meeComponentRegistry.getInstanceById("PrivacySubjectSelector"));
        this.groundControlApiService = groundControlApiServiceParam;
    }

    private getGroundControl(): ng.IPromise<void> {
        return this.groundControlApiService.getUserFlights().then(userFlights => {
            this.isPRCFlagEnabled = userFlights.data.includes(PRCFeatureFlag);
        });
    }

    public $onInit(): void {
        if (!this.errorCategory) {
            this.errorCategory = defaultErrorCategory;
        }

        this.getGroundControl();
        
        this.resetForm();
        
        this.parentCtrl.getInstance().privacySubjectChanged();
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister(this.identifierType);
        
        this.parentCtrl.getInstance().privacySubjectChanged();
    }

    public hasDataEntryErrors(): boolean {
        if (this.isEmailMandatory() && this.getResourcesId(this.emailsSelectorData).length === 0) {
            this.pcdError.setErrorForId(`${this.errorCategory}.email-id`, "Email is mandatory");
            return true;
        }

        return false;
    }

    public isEmailMandatory(): boolean {
        return this.isPRCFlagEnabled;
    }

    public resetErrors(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
    }

    public resetForm(): void {
        this.resetErrors();
        this.identifierFormData = {
            kind: "Demographic",
            names: null,
            emails: null,
            phoneNumbers: null,
            postalAddress: {
                unitNumbers: null,
                streetNumbers: null,
                streetNames: null,
                cities: null,
                regions: null,
                postalCodes: null
            }
        };
    }

    public getIdentifierFormData(): ManualRequests.DemographicSubject {
        this.identifierFormData.names = this.getResourcesId(this.namesSelectorData);
        this.identifierFormData.emails = this.getResourcesId(this.emailsSelectorData);
        this.identifierFormData.phoneNumbers = this.getResourcesId(this.phoneNumbersSelectorData);
        this.identifierFormData.postalAddress.unitNumbers = this.getResourcesId(this.unitNumbersSelectorData);
        this.identifierFormData.postalAddress.streetNumbers = this.getResourcesId(this.streetNumbersSelectorData);
        this.identifierFormData.postalAddress.streetNames = this.getResourcesId(this.streetNamesSelectorData);
        this.identifierFormData.postalAddress.cities = this.getResourcesId(this.citiesSelectorData);
        this.identifierFormData.postalAddress.regions = this.getResourcesId(this.regionsSelectorData);
        this.identifierFormData.postalAddress.postalCodes = this.getResourcesId(this.postalCodesSelectorData);
        return this.identifierFormData;
    }

    private getResourcesId(selectorData: NamedResourceSelectorData): string[] {
        return selectorData ? _.map(selectorData.resources, (resource: ResourceEntity) => resource.id) : [];
    }
}
