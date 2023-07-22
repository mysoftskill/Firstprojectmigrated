import { Component, Inject, Route } from "../../../../../module/app.module";
import template = require("./create-team.html!text");

import * as Pdms from "../../../../../shared/pdms/pdms-types";
import ServiceTreeSelectorComponent from "../../../../shared/service-tree-selector/service-tree-selector.component";
import { IOwnerIdContextService } from "../../../../../shared/owner-id-context.service";
import { IPcdErrorService, PcdErrorOverrides } from "../../../../../shared/pcd-error.service";
import { SecurityGroupSelectorData, SecurityGroup, ApplicationSelectorData, Application } from "../../../../shared/directory-resource-selector/directory-resource-selector-types";

import { IAngularFailureResponse } from "../../../../../shared/ajax.service";
import { Lazy } from "../../../../../shared/utilities/lazy";

const useCmsHere_PageHeading = "Register new team from Service Tree";
const useCmsHere_WriteGroupsLabel = "Write security groups";
const useCmsHere_TagGroupsLabel = "Tagging security groups";
const useCmsHere_TagApplicationsLabel = "Tagging application ids";

const useCmsHere_FieldRequired = "This field is required.";
const useCmsHere_InvalidGroups = "Some of the security groups are not valid and cannot be used. Please remove invalid entries.";

const useCmsHere_ServiceTreeIdInUse = "The Service Tree service you selected is already in use. Please contact the administrator of that service and allow up to 24 hours for Service Tree changes to update in our system.";

const Service_Owner_Error_Overrides: PcdErrorOverrides = {
    overrides: {
        errorMessages: {
            "alreadyExists": useCmsHere_ServiceTreeIdInUse
        },
        targetErrorIds: {
            "writeSecurityGroups": "write-security-groups"
        }
    },
    genericErrorId: "save"
};

interface FieldErrorArgs {
    securityGroups: SecurityGroup[];
    errorId: string;
    isRequiredField: boolean;
    invalidErrorString: string;
}

@Component({
    name: "pcdCreateServiceTreeTeam",
    options: {
        template
    }
})
@Inject("ownerIdContextService", "pcdErrorService", "pdmsDataService", "$state", "$meeComponentRegistry")
export default class CreateServiceTreeTeamComponent implements ng.IComponentController, Pdms.ServiceTreeSelectorParent {
    public errorCategory = "create-service-tree-team";

    public pageHeading = useCmsHere_PageHeading;
    public writeGroupsLabel = useCmsHere_WriteGroupsLabel;
    public tagGroupsLabel = useCmsHere_TagGroupsLabel;
    public tagApplicationsLabel = useCmsHere_TagApplicationsLabel;

    public securityGroupSelectorData: SecurityGroupSelectorData = { securityGroups: []};
    public tagGroupSelectorData: SecurityGroupSelectorData = { securityGroups: [] };
    public tagApplicationSelectorData: ApplicationSelectorData = { applications: [] };

    public service: Pdms.STServiceDetails;
    private serviceTreeSelector: Lazy<ServiceTreeSelectorComponent>;

    private associatedDataOwners: Pdms.DataOwner[];
    private dataOwner: Pdms.DataOwner = {
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
        tagApplicationIds: [],
        serviceTree: {
            id: "",
            kind: "service"
        }
    };

    constructor(
        private readonly ownerIdContextService: IOwnerIdContextService,
        private readonly pcdError: IPcdErrorService,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService
    ) {
        this.$meeComponentRegistry.register("ServiceTreeSelectorParent", "CreateServiceTreeTeamComponent", this);

        this.serviceTreeSelector = new Lazy<ServiceTreeSelectorComponent>(() => 
            this.$meeComponentRegistry.getInstanceById<ServiceTreeSelectorComponent>("ServiceTreeSelectorComponent"));
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("createServiceTreeTeamComponent")
    public $onInit(): ng.IPromise<any> {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        return this.pdmsDataService.getOwnersByAuthenticatedUser()
            .then((owners: Pdms.DataOwner[]) => {
                this.associatedDataOwners = owners;
            });
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister("CreateServiceTreeTeamComponent");
    }

    public getExistingPdmsOwnerId(): string {
        let owner = _.find(this.associatedDataOwners,
            (o: Pdms.DataOwner) => o.name === this.serviceTreeSelector.getInstance().service.name);
        return owner && owner.id;
    }

    public isUpdateAllowed(): boolean {
        return this.serviceTreeSelector.getInstance().isAdminOfService();
    }

    private isValidationSuccessful(): boolean {
        // Write security groups validation.
        this.setErrorsForField({
            securityGroups: this.securityGroupSelectorData.securityGroups,
            isRequiredField: true,
            errorId: `${this.errorCategory}.write-security-groups`,
            invalidErrorString: useCmsHere_InvalidGroups
        });

        // Tagging security groups validation.
        this.setErrorsForField({
            securityGroups: this.tagGroupSelectorData.securityGroups,
            isRequiredField: false,
            errorId: `${this.errorCategory}.tag-security-groups`,
            invalidErrorString: useCmsHere_InvalidGroups
        });

        return !this.pcdError.hasErrorsInCategory(this.errorCategory);
    }

    public saveClicked(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        if (this.isValidationSuccessful()) {
            if (this.service) {
                this.dataOwner.serviceTree = {
                    id: this.service.id,
                    kind: this.service.kind
                };
            } else {
                this.dataOwner.serviceTree = null;
            }
            this.dataOwner.writeSecurityGroups = _.map(this.securityGroupSelectorData.securityGroups, (sg: SecurityGroup) => sg.id);
            this.dataOwner.tagSecurityGroups = _.map(this.tagGroupSelectorData.securityGroups, (sg: SecurityGroup) => sg.id);
            this.dataOwner.tagApplicationIds = _.map(this.tagApplicationSelectorData.applications, (application: Application) => application.id);

            this.save();
        }
    }

    private setErrorsForField(args: FieldErrorArgs): void {
        if (args.isRequiredField && !args.securityGroups.length) {
            this.pcdError.setErrorForId(`${args.errorId}`, useCmsHere_FieldRequired);
        }
        if (_.some(args.securityGroups, group => group.isInvalid)) {
            this.pcdError.setErrorForId(`${args.errorId}`, args.invalidErrorString);
        }
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("serviceTreeOwnerCreate")
    private save(): ng.IPromise<void> {
        return this.pdmsDataService.updateDataOwner(this.dataOwner)
            .then(data => {
                this.ownerIdContextService.setActiveOwnerId(this.dataOwner.id);
                this.$state.go("landing", {}, { location: "replace", reload: true });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, Service_Owner_Error_Overrides);
            });
    }
}
