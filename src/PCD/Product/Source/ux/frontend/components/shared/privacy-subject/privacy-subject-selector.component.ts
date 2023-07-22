import { Component, Inject } from "../../../module/app.module";
import template = require("./privacy-subject-selector.html!text");

import * as ManualRequest from "../../../shared/manual-requests/manual-request-types";
import * as Shared from "../../../shared/shared-types";
import * as SelectList from "../../../shared/select-list";
import { Lazy } from "../../../shared/utilities/lazy";
import { IGroundControlApiService } from "../../../shared/flighting/ground-control-api.service";

const useCmsHere_MsaPrivacySubjectLabel = "MSA";
const useCmsHere_MicrosoftEmployeePrivacySubjectLabel = "Microsoft employee";
const useCmsHere_DemographicPrivacySubjectLabel = "Alternate subject";

const subjectTypeFeatureFlag = "PCD.PRCAllowMicrosoftEmployeeAndAltIdSubject";

type IdentifierFormsDictionary = {
    [key: string]: Shared.IDataEntryForm
};

@Component({
    name: "pcdPrivacySubjectSelector",
    options: {
        template,
        bindings: {
            errorCategory: "@?pcdErrorCategory"
        }
    }
})
@Inject("$meeComponentRegistry", "groundControlApiService")
export default class PrivacySubjectSelector implements ng.IComponentController {
    public errorCategory: string;

    private privacySubjectPickerModel: SelectList.Model;
    private selectedIdentifierType: string;
    private groundControlApiService: IGroundControlApiService;

    private isMSandAltIdSubjectEnabled:boolean;

    // Parent controllers.
    public parentCtrl: Lazy<ManualRequest.PrivacySubjectSelectorParent>;

    // Child controllers.
    public identifierForms: IdentifierFormsDictionary = {};

    constructor(private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService, private readonly groundControlApiServiceParam: IGroundControlApiService) {
        this.$meeComponentRegistry.register("PrivacySubjectSelector", "PrivacySubjectSelector", this);
        this.groundControlApiService = groundControlApiServiceParam;
        this.isMSandAltIdSubjectEnabled = null;
        this.groundControlApiService.getUserFlights().then(userFlights => {
            this.isMSandAltIdSubjectEnabled = userFlights.data.includes(subjectTypeFeatureFlag);
            this.updatePrivacySubjectPickerModel();
        });
        this.parentCtrl = new Lazy<ManualRequest.PrivacySubjectSelectorParent>(() => {
            let response = this.$meeComponentRegistry
                .getInstancesByClass<ManualRequest.PrivacySubjectSelectorParent>("PrivacySubjectSelectorParent");

            if (response.length !== 1) {
                throw new Error("Only one parent is supported, please ensure the parent" +
                    "registers with $meeComponentRegistry in the constructor and deregisters in $onDestroy.");
            }
            return response.pop();
        });
    }
    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister("PrivacySubjectSelector");
    }

    public $onInit(): void {
        SelectList.enforceModelConstraints(this.privacySubjectPickerModel);
    }

    public privacySubjectChanged(): void {
        this.updateIdentifierForms();

        _.forEach(_.keys(this.identifierForms), (key: string) => {
            this.identifierForms[key].resetForm();
        });
        this.parentCtrl.getInstance().privacySubjectChanged();
    }

    public resetErrors(): void {
        _.forEach(_.keys(this.identifierForms), (key: string) => {
            this.identifierForms[key].resetErrors();
        });
    }

    public isValidationSuccessful(): boolean {
        return !this.identifierForms[this.privacySubjectPickerModel.selectedId] ||
            !this.identifierForms[this.privacySubjectPickerModel.selectedId].hasDataEntryErrors();
    }

    public getIdentifierFormData(): ManualRequest.PrivacySubjectIdentifier {
        return this.identifierForms[this.privacySubjectPickerModel.selectedId] &&
            this.identifierForms[this.privacySubjectPickerModel.selectedId].getIdentifierFormData();
    }

    public isMsaFormVisible(): boolean {
        return this.privacySubjectPickerModel.selectedId === ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.MSA];
    }
    public isMicrosoftEmployeeFormVisible(): boolean {
        return this.privacySubjectPickerModel.selectedId === ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.MicrosoftEmployee];
    }
    public isDemographicFormVisible(): boolean {
        return this.privacySubjectPickerModel.selectedId === ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.Demographic];
    }

    private isMicrosoftEmployeeEnabled(): boolean {
        return this.isMSandAltIdSubjectEnabled;
    }

    private updatePrivacySubjectPickerModel(): void {
        if(this.isMicrosoftEmployeeEnabled()) {    
            this.privacySubjectPickerModel = {
                selectedId: ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.Demographic],
                items: [{
                    id: ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.MSA],
                    label: useCmsHere_MsaPrivacySubjectLabel
                }, {
                    id: ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.MicrosoftEmployee],
                    label: useCmsHere_MicrosoftEmployeePrivacySubjectLabel
                }, {
                    id: ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.Demographic],
                    label: useCmsHere_DemographicPrivacySubjectLabel
                }]
            };
        } else {   
            this.privacySubjectPickerModel = {
                selectedId: ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.MSA],
                items: [{
                    id: ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.MSA],
                    label: useCmsHere_MsaPrivacySubjectLabel
                }]
            };
        }
    }

    private updateIdentifierForms(): void {
        this.identifierForms = {};
        this.$meeComponentRegistry.getInstancesByClass("PrivacySubject")
            .forEach((privacySubjectIdentifier: ManualRequest.PrivacySubjectIdentifierFormEntryComponent) => {
                this.identifierForms[privacySubjectIdentifier.identifierType] = privacySubjectIdentifier;
            });
    }
}
