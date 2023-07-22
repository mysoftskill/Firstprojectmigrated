import { Component, Inject } from "../../../../../module/app.module";
import template = require("./edit-team.html!text");

import * as Pdms from "../../../../../shared/pdms/pdms-types";
import { IOwnerIdContextService } from "../../../../../shared/owner-id-context.service";
import { IPcdErrorService, PcdErrorOverrides } from "../../../../../shared/pcd-error.service";
import { SecurityGroupSelectorData, SecurityGroup, ContactSelectorData, Contact, GraphResourceEntity, ApplicationSelectorData } from "../../../../shared/directory-resource-selector/directory-resource-selector-types";
import { EditOwnerStateParams } from "./../../management-flows";
import { DeleteTeamConfirmationModalData } from "../delete-team-confirmation.component";
import * as Guid from "../../../../../shared/guid";
import { IContactService } from "../../../../../shared/contact.service";

import { IAngularFailureResponse } from "../../../../../shared/ajax.service";
import { DataOwnerHelper } from "../../../../../shared/data-owner-helper";
import { Application } from "../../../../../shared/graph/graph-types";

const useCmsHere_PageHeading = "Edit team";
const useCmsHere_WriteGroupsLabel = "Write security groups";
const useCmsHere_TagGroupsLabel = "Tagging security groups";
const useCmsHere_TagApplicationsLabel = "Tagging applications";
const useCmsHere_SharingRequestContactsLabel = "Sharing request contacts";

const useCmsHere_FieldRequired = "This field is required.";
const useCmsHere_InvalidGroups = "Some of the security groups are not valid and cannot be used. Please remove invalid entries.";
const useCmsHere_InvalidAppIds = "Some of the applications are not valid and cannot be used. Please remove invalid entries.";
const useCmsHere_InvalidSharingRequestContacts = "Some of the sharing request contacts are not valid and cannot be used. Please remove invalid entries.";
const useCmsHere_IcmConnectorNotGuid = "IcM connector ID that you have entered doesn't look like a valid GUID. Please check your input.";

const Service_Owner_Error_Overrides: PcdErrorOverrides = {
    overrides: {
        targetErrorIds: {
            "writeSecurityGroups": "write-security-groups",
            "icm.connectorId": "icm-connector"
        }
    },
    genericErrorId: "save"
};

interface FieldErrorArgs {
    entities: GraphResourceEntity[];
    errorId: string;
    isRequiredField: boolean;
    invalidErrorString: string;
}

@Component({
    name: "pcdEditServiceTreeTeam",
    options: {
        template
    }
})
@Inject("ownerIdContextService", "pcdErrorService", "pdmsDataService", "$state", "$stateParams", "$meeModal", "contactService", "$q")
export default class EditServiceTreeTeamComponent implements ng.IComponentController {
    public errorCategory = "edit-service-tree-team";

    public pageHeading = useCmsHere_PageHeading;
    public writeGroupsLabel = useCmsHere_WriteGroupsLabel;
    public tagGroupsLabel = useCmsHere_TagGroupsLabel;
    public tagApplicationsLabel = useCmsHere_TagApplicationsLabel;
    public sharingRequestContactsLabel = useCmsHere_SharingRequestContactsLabel;

    public owner: Pdms.DataOwner;
    public serviceTreeData: Pdms.STServiceDetails;
    public sharingRequestContactsSelectorData: ContactSelectorData;
    public securityGroupSelectorData: SecurityGroupSelectorData;
    public tagGroupSelectorData: SecurityGroupSelectorData;
    public tagApplicationSelectorData: ApplicationSelectorData;

    private hasAttemptedToSave = false;
    private isDeleteEnabled = false;
    
    public initializedWithSharingContacts: boolean;
    private assetCount: number;
    private agentsCount: number;

    constructor(
        private readonly ownerIdContextService: IOwnerIdContextService,
        private readonly pcdError: IPcdErrorService,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $stateParams: EditOwnerStateParams,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly contactService: IContactService,
        private readonly $q: ng.IQService) { }

    public $onInit(): ng.IPromise<any> {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        this.owner = this.$stateParams.dataOwner;
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
            applications: this.owner.tagApplicationIds.map((id: string) => {
                return {
                    id: id,
                    displayName: ""
                };
            })
        };

        this.initializedWithSharingContacts = this.sharingRequestContactsSelectorData.contacts.length > 0;

        this.serviceTreeData = { ...<Pdms.STServiceDetails>this.owner.serviceTree, name: this.owner.name, description: this.owner.description };

        return this.$q.all([
            this.getDataAssetsCountForOwner(),
            this.getDataAgentsCountForOwner()
        ]).then( () => {
            this.isDeleteEnabled = (this.agentsCount === 0 && this.assetCount === 0);
            return Promise.resolve();
        } );
    }

    public showSuccessfulLinkBanner(): boolean {
        return this.$stateParams.isNewlyLinked && !this.hasAttemptedToSave;
    }

    public isOrphanServiceTreeTeam(): boolean {
        return DataOwnerHelper.hasOrphanedServiceTeam(this.owner);
    }

    public showLinkToServiceTreeModalDialog(): void {
        this.$meeModal.setData(this.owner);
        this.$meeModal.show("#modal-dialog", ".link-service-tree");
    }

    public saveClicked(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        this.hasAttemptedToSave = true;

        if (this.isValidationSuccessful()) {
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

    public requestMoveTeamAssets(): void {
        this.contactService.requestAdminAssistance("move-team-assets", {
            entityId: this.owner.id
        });
    }

    private isValidationSuccessful(): boolean {
        // Sharing Request contacts validation
        if (!this.sharingRequestContactsSelectorData.contacts.length && this.initializedWithSharingContacts) {
            this.pcdError.setErrorForId(`${this.errorCategory}.sharing-request-contacts`, useCmsHere_FieldRequired);
        }
        if (_.some(this.sharingRequestContactsSelectorData.contacts, contact => contact.isInvalid)) {
            this.pcdError.setErrorForId(`${this.errorCategory}.sharing-request-contacts`, useCmsHere_InvalidSharingRequestContacts);
        }

        // Write security groups validation
        this.setErrorsForField({
            entities: this.securityGroupSelectorData.securityGroups,
            isRequiredField: true,
            errorId: `write-security-groups`,
            invalidErrorString: useCmsHere_InvalidGroups
        });

        // Tagging security groups validation
        this.setErrorsForField({
            entities: this.tagGroupSelectorData.securityGroups,
            isRequiredField: false,
            errorId: `tag-security-groups`,
            invalidErrorString: useCmsHere_InvalidGroups
        });

        // Tagging application ids validation
        this.setErrorsForField({
            entities: this.tagApplicationSelectorData.applications,
            isRequiredField: false,
            errorId: `tag-application-ids`,
            invalidErrorString: useCmsHere_InvalidAppIds
        });

        if (this.owner.icmConnectorId && !Guid.isValidGuid(this.owner.icmConnectorId)) {
            this.pcdError.setErrorForId(`${this.errorCategory}.icm-connector`, useCmsHere_IcmConnectorNotGuid);
        }

        return !this.pcdError.hasErrorsInCategory(this.errorCategory);
    }

    private setErrorsForField(args: FieldErrorArgs): void {
        if (args.isRequiredField && !args.entities.length) {
            this.pcdError.setErrorForId(`${this.errorCategory}.${args.errorId}`, useCmsHere_FieldRequired);
        }
        if (_.some(args.entities, e => e.isInvalid)) {
            this.pcdError.setErrorForId(`${this.errorCategory}.${args.errorId}`, args.invalidErrorString);
        }
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("serviceTreeOwnerUpdate")
    private save(): ng.IPromise<void> {
        return this.pdmsDataService.updateDataOwner(this.owner)
            .then(data => {
                this.ownerIdContextService.setActiveOwnerId(this.owner.id);
                this.$state.go("landing", {}, { location: "replace", reload: true });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, Service_Owner_Error_Overrides);
            });
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchDataAssetsCountForOwner")
    public getDataAssetsCountForOwner(): ng.IPromise<void> {
        return this.pdmsDataService.getAssetGroupsCountByOwnerId(this.owner.id)
            .then((count: number) => {
                this.assetCount = count;
            });
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchDataAgentsCountForOwner")
    public getDataAgentsCountForOwner(): ng.IPromise<void> {
        return this.pdmsDataService.getDataAgentsCountByOwnerId(this.owner.id)
            .then((count: number) => {
                this.agentsCount = count;
            });
    }
}
