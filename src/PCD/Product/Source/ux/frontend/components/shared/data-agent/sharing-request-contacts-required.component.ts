import * as angular from "angular";
import { Component, Inject } from "../../../module/app.module";
import template = require("./sharing-request-contacts-required.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { ConfirmationModalData } from "./../utilities/confirmation-modal-actions.component";
import { ContactSelectorData, Contact } from "../directory-resource-selector/directory-resource-selector-types";
import { IAngularFailureResponse } from "../../../shared/ajax.service";
import { IPcdErrorService, PcdErrorOverrides } from "../../../shared/pcd-error.service";

interface SharingContactsConfirmationModalData {
    owner: Pdms.DataOwner;
    returnLocation: string;
}

const useCmsHere_GenericSharingRequestErrorMessage = "We are not able to change your team record at the moment. Please refresh the page and try again.";
const useCmsHere_SharingRequestContactsLabel = "Sharing request contacts";
const useCmsHere_FieldRequired = "This field is required.";
const useCmsHere_InvalidSharingRequestContacts = "Some of the sharing request contacts are not valid and cannot be used. Please remove invalid entries.";

const Owner_Editor_Error_Overrides: PcdErrorOverrides = {
    overrides: {
        genericErrorMessage: useCmsHere_GenericSharingRequestErrorMessage
    },
    genericErrorId: "save"
};

@Component({
    name: "pcdSharingRequestContactsRequired",
    options: {
        template
    }
})
@Inject("pdmsDataService", "pcdErrorService", "$meeModal")
export default class SharingRequestContactsRequiredComponent implements ng.IComponentController {
    public owner: Pdms.DataOwner;
    public data: any;
    public errorCategory = "sharing-request-contacts-required";

    public sharingRequestContactsLabel = useCmsHere_SharingRequestContactsLabel;
    public sharingRequestContactsSelectorData: ContactSelectorData;

    constructor(
        private readonly pdmsData: Pdms.IPdmsDataService,
        private readonly pcdError: IPcdErrorService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        this.data = this.$meeModal.getData<SharingContactsConfirmationModalData>();

        // Need to create a copy of the owner data so if a user doesn't save the contact information
        // it doesn't get used in the agent-editor page.
        this.owner = angular.copy(this.data.owner);

        this.pcdError.resetErrorsForCategory(this.errorCategory);

        this.sharingRequestContactsSelectorData = {
            contacts: this.owner.sharingRequestContacts.map((contactEmail: string) => {
                return {
                    id: null,
                    displayName: "",
                    email: contactEmail
                };
            })
        };
    }

    private isValidationSuccessful(): boolean {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        // Sharing Request contacts validation
        if (!this.sharingRequestContactsSelectorData.contacts.length) {
            this.pcdError.setErrorForId(`${this.errorCategory}.sharing-request-contacts`, useCmsHere_FieldRequired);
        }
        if (_.some(this.sharingRequestContactsSelectorData.contacts, contact => contact.isInvalid)) {
            this.pcdError.setErrorForId(`${this.errorCategory}.sharing-request-contacts`, useCmsHere_InvalidSharingRequestContacts);
        }

        return !this.pcdError.hasErrorsInCategory(this.errorCategory);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    public onSave(): ng.IPromise<any> {
        if (this.isValidationSuccessful()) {
            this.owner.sharingRequestContacts = _.map(this.sharingRequestContactsSelectorData.contacts,
                (contact: Contact) => contact.email);

            return this.pdmsData.updateDataOwner(this.owner)
                .then((updatedOwner: Pdms.DataOwner) => {
                    this.data.owner = updatedOwner;
                    this.$meeModal.hide({ stateId: this.data.returnLocation });
                })
                .catch((e: IAngularFailureResponse) => {
                    this.pcdError.setError(e, this.errorCategory, Owner_Editor_Error_Overrides);
                });
        }
    }
}
