import { Component, Inject } from "../../../../module/app.module";
import template = require("./owner-editor.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { IOwnerIdContextService } from "../../../../shared/owner-id-context.service";
import { SecurityGroupSelectorData, ContactSelectorData, Contact, SecurityGroup, GraphResourceEntity, Application, ApplicationSelectorData } from "../../../shared/directory-resource-selector/directory-resource-selector-types";
import { IGraphDataService } from "../../../../shared/graph/graph-data.service";
import { IPcdErrorService, PcdErrorOverrides } from "../../../../shared/pcd-error.service";
import * as Guid from "../../../../shared/guid";
import { IContactService } from "../../../../shared/contact.service";

import { IAngularFailureResponse } from "../../../../shared/ajax.service";
import { ErrorCodeHelper } from "../../../shared/utilities/error-code-helper";

import { DeleteTeamConfirmationModalData } from "./delete-team-confirmation.component";

type OnSavedArgs = {
    updatedOwner: Pdms.DataOwner
};

const useCmsHere_AlertContactsLabel = "Alert contacts";
const useCmsHere_AnnouncementContactsLabel = "Announcement contacts";
const useCmsHere_WriteGroupsLabel = "Write security groups";
const useCmsHere_TagGroupsLabel = "Tagging security groups";
const useCmsHere_TagApplicationsLabel = "Tagging applications";
const useCmsHere_SharingRequestContactsLabel = "Sharing request contacts";

const useCmsHere_FieldRequired = "This field is required.";
const useCmsHere_InvalidGroups = "Some of the security groups are not valid and cannot be used. Please remove invalid entries.";
const useCmsHere_InvalidAppIds = "Some of the applications are not valid and cannot be used. Please remove invalid entries.";
const useCmsHere_InvalidAlertContacts = "Some of the alert contacts are not valid and cannot be used. Please remove invalid entries.";
const useCmsHere_InvalidAnnouncementContacts = "Some of the announcement are not valid and cannot be used. Please remove invalid entries.";
const useCmsHere_InvalidSharingRequestContacts = "Some of the sharing request contacts are not valid and cannot be used. Please remove invalid entries.";
const useCmsHere_IcmConnectorNotGuid = "IcM connector ID that you have entered doesn't look like a valid GUID. Please check your input.";

const useCmsHere_DataOwnerAlreadyExists = "The team you're trying to add already exists.";
const useCmsHere_HasPendingTransferRequests = "The team you're trying to update has some pending transfer requests.";
const useCmsHere_HasInitiatedTransferRequests = "The team you're trying to update has some initiated transfer requests";

const Owner_Editor_Error_Overrides: PcdErrorOverrides = {
    overrides: {
        errorMessages: {
            "alreadyExists": useCmsHere_DataOwnerAlreadyExists,
            "hasPendingTransferRequests": useCmsHere_HasPendingTransferRequests,
            "hasInitiatedTransferRequests": useCmsHere_HasInitiatedTransferRequests
        },
        targetErrorIds: {
            "alertContacts": "alert-contacts",
            "announcementContacts": "announcement-contacts",
            "writeSecurityGroups": "write-security-groups",
            "icm.connectorId": "icm-connector"
        }
    },
    genericErrorId: "save"
};

interface FieldErrorArgs {
    /** 
     * Array of Graph entities like contacts or SGs. 
     **/
    entities: GraphResourceEntity[];
    errorId: string;
    isRequiredField: boolean;
    invalidErrorString: string;
}

@Component({
    name: "pcdOwnerEditor",
    options: {
        template,
        bindings: {
            owner: "<",
            createNew: "<",
            onSaved: "&"
        }
    }
})
@Inject("pdmsDataService", "graphDataService", "pcdErrorService", "$state", "$meeModal", "ownerIdContextService", "contactService")
class DataOwnerEditorComponent implements ng.IComponentController {
    /** 
     * Input: data owner instance to be edited. If falsy and createNew is set to true, new data owner object will be created. 
     **/
    public owner: Pdms.DataOwner;
    /** 
     * Input: if true and owner is falsy, new data owner object will be created. 
     **/
    public createNew: boolean;
    /** 
     * Input: callback that occurs after data owner object was successfully saved. 
     **/
    public onSaved: (args: OnSavedArgs) => void;

    public errorCategory = "owner-editor";

    public alertContactsLabel = useCmsHere_AlertContactsLabel;
    public announcementContactsLabel = useCmsHere_AnnouncementContactsLabel;
    public writeGroupsLabel = useCmsHere_WriteGroupsLabel;
    public tagGroupsLabel = useCmsHere_TagGroupsLabel;
    public tagApplicationsLabel = useCmsHere_TagApplicationsLabel;
    public sharingRequestContactsLabel = useCmsHere_SharingRequestContactsLabel;

    public securityGroupSelectorData: SecurityGroupSelectorData;
    public tagGroupSelectorData: SecurityGroupSelectorData;
    public tagApplicationSelectorData: ApplicationSelectorData;
    public alertContactsSelectorData: ContactSelectorData;
    public announcementContactsSelectorData: ContactSelectorData;
    public sharingRequestContactsSelectorData: ContactSelectorData;

    public initializedWithSharingContacts: boolean;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly graphData: IGraphDataService,
        private readonly pcdError: IPcdErrorService,
        private readonly $state: ng.ui.IStateService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly ownerIdContextService: IOwnerIdContextService,
        private readonly contactService: IContactService) { }

    public $onInit(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        if (!this.owner) {
            if (!this.createNew) {
                throw new Error("No data owner was provided, and creation of a new data owner was not requested.");
            }

            this.owner = {
                id: "",
                name: "",
                description: "",
                alertContacts: [],
                announcementContacts: [],
                sharingRequestContacts: [],
                assetGroups: [],
                dataAgents: [],
                writeSecurityGroups: [],
                tagSecurityGroups: [],
                serviceTree: null
            };
        }

        this.alertContactsSelectorData = {
            contacts: this.owner.alertContacts.map((contactEmail: string) => {
                return {
                    id: null,
                    displayName: "",
                    email: contactEmail
                };
            })
        };

        this.announcementContactsSelectorData = {
            contacts: this.owner.announcementContacts.map((contactEmail: string) => {
                return {
                    id: null,
                    displayName: "",
                    email: contactEmail
                };
            })
        };

        this.sharingRequestContactsSelectorData = {
            contacts: this.owner.sharingRequestContacts.map((contactEmail: string) => {
                return {
                    id: null,
                    displayName: "",
                    email: contactEmail
                };
            })
        };

        this.securityGroupSelectorData = {
            securityGroups: this.owner.writeSecurityGroups.map((groupId: string) => {
                return {
                    id: groupId,
                    displayName: "",
                    email: ""
                };
            })
        };

        this.tagGroupSelectorData = {
            securityGroups: this.owner.tagSecurityGroups.map((groupId: string) => {
                return {
                    id: groupId,
                    displayName: "",
                    email: ""
                };
            })
        };

        this.tagApplicationSelectorData = {
            applications: this.owner.tagApplicationIds.map((appId: string) => {
                return {
                    id: appId,
                    displayName: ""
                };
            })
        };

        this.initializedWithSharingContacts = this.sharingRequestContactsSelectorData.contacts.length > 0;
    }

    public saveClicked(): void {
        //  TODO: save button must be disabled, if SG wasn't populated.
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        if (this.isValidationSuccessful()) {
            this.owner.alertContacts = _.map(this.alertContactsSelectorData.contacts,
                (contact: Contact) => contact.email);
            this.owner.announcementContacts = _.map(this.announcementContactsSelectorData.contacts,
                (contact: Contact) => contact.email);
            this.owner.writeSecurityGroups = _.map(this.securityGroupSelectorData.securityGroups,
                (sg: SecurityGroup) => sg.id);
            this.owner.tagSecurityGroups = _.map(this.tagGroupSelectorData.securityGroups,
                (sg: SecurityGroup) => sg.id);
            this.owner.tagApplicationIds = _.map(this.tagApplicationSelectorData.applications,
                (app: Application) => app.id);
            this.owner.sharingRequestContacts = _.map(this.sharingRequestContactsSelectorData.contacts,
                (contact: Contact) => contact.email);

            this.save();
        }
    }

    public deleteTeamClicked(): void {
        let data: DeleteTeamConfirmationModalData = {
            owner: this.owner,
            onConfirm: () => {
                return this.pdmsDataService.deleteDataOwner(this.owner).then(() => this.ownerIdContextService.setActiveOwnerId(""));
            },
            returnLocation: {
                stateId: "landing",
                stateOptions: {
                    reload: true,
                    location: "replace"
                }
            },
            returnLocationOnCancel: "^"
        };
        this.$meeModal.show("#modal-dialog", ".delete-team", { data });
    }

    private isValidationSuccessful(): boolean {
        if (!this.owner.name) {
            this.pcdError.setErrorForId(`${this.errorCategory}.name`, useCmsHere_FieldRequired);
        }
        if (!this.owner.description) {
            this.pcdError.setErrorForId(`${this.errorCategory}.description`, useCmsHere_FieldRequired);
        }

        if (!this.owner.icmConnectorId) {
            this.pcdError.setErrorForId(`${this.errorCategory}.icm-connector`, useCmsHere_FieldRequired);
        }

        if (this.owner.icmConnectorId && !Guid.isValidGuid(this.owner.icmConnectorId)) {
            this.pcdError.setErrorForId(`${this.errorCategory}.icm-connector`, useCmsHere_IcmConnectorNotGuid);
        }

        // Security groups validation
        this.setErrorsForField({
            entities: this.securityGroupSelectorData.securityGroups,
            errorId: "write-security-groups",
            isRequiredField: true,
            invalidErrorString: useCmsHere_InvalidGroups
        });

        // Tag groups validation
        this.setErrorsForField({
            entities: this.tagGroupSelectorData.securityGroups,
            errorId: "tag-security-groups",
            isRequiredField: false,
            invalidErrorString: useCmsHere_InvalidGroups
        });

        // Tag applications validation
        this.setErrorsForField({
            entities: this.tagApplicationSelectorData.applications,
            errorId: "tag-applications",
            isRequiredField: false,
            invalidErrorString: useCmsHere_InvalidAppIds
        });

        // Alert contacts validation
        this.setErrorsForField({
            entities: this.alertContactsSelectorData.contacts,
            errorId: "alert-contacts",
            isRequiredField: true,
            invalidErrorString: useCmsHere_InvalidAlertContacts
        });

        // Announcement contacts validation
        this.setErrorsForField({
            entities: this.announcementContactsSelectorData.contacts,
            errorId: "announcement-contacts",
            isRequiredField: true,
            invalidErrorString: useCmsHere_InvalidAnnouncementContacts
        });

        // Sharing Request contacts validation
        if (!this.sharingRequestContactsSelectorData.contacts.length && this.initializedWithSharingContacts) {
            this.pcdError.setErrorForId(`${this.errorCategory}.sharing-request-contacts`, useCmsHere_FieldRequired);
        }
        if (_.some(this.sharingRequestContactsSelectorData.contacts, contact => contact.isInvalid)) {
            this.pcdError.setErrorForId(`${this.errorCategory}.sharing-request-contacts`, useCmsHere_InvalidSharingRequestContacts);
        }

        return !this.pcdError.hasErrorsInCategory(this.errorCategory);
    }

    public requestMoveTeamAssets(): void {
        this.contactService.requestAdminAssistance("move-team-assets", {
            entityId: this.owner.id
        });
    }

    //  TODO: Move this inside DRS once it uses Orchestrator pattern for communication.
    private setErrorsForField(args: FieldErrorArgs): void {
        if (args.isRequiredField && !args.entities.length) {
            this.pcdError.setErrorForId(`${this.errorCategory}.${args.errorId}`, useCmsHere_FieldRequired);
        }
        if (_.some(args.entities, entity => entity.isInvalid)) {
            this.pcdError.setErrorForId(`${this.errorCategory}.${args.errorId}`, args.invalidErrorString);
        }
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("ownerEditorSave")
    private save(): ng.IPromise<any> {
        return this.pdmsDataService.updateDataOwner(this.owner)
            .then(data => {
                this.onSaved({ updatedOwner: data });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, Owner_Editor_Error_Overrides);
            });
    }
}
