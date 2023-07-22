import { Component, Inject } from "../../../module/app.module";
import template = require("./directory-resource-view.html!text");

import * as GraphTypes from "../../../shared/graph/graph-types";
import { IGraphDataService } from "../../../shared/graph/graph-data.service";
import * as SharedTypes from "../../../shared/shared-types";

const useCmsHere_EmailAllLabel = "Email all";
const useCmsHere_EmailIndividualLabel = "Send email";
const useCmsHere_InvalidLabel = "This resource could not be resolved, and may require your attention.";

type SupportedResourceClass = "invalid" | "interaction-enabled" | "interaction-disabled";

type SupportedResourceTypes = GraphTypes.Contact | GraphTypes.User | GraphTypes.Group;

interface Resource {
    displayName: string;
    email: string;
    isInvalid: boolean;
}

@Component({
    name: "pcdDirectoryResourceView",
    options: {
        template,
        bindings: {
            ngModel: "<",
            type: "@",
            actionableKind: "@?",
            displayLabel: "@resourceLabel"
        }
    }
})

/**  
 * This component only supports "email" and "security-group" types currently. 
 **/
@Inject("graphDataService", "$q", "$window", "$meeMonitoredOperation", "$meeUtil")
export default class DirectoryResourceViewComponent implements ng.IComponentController {
    /** 
     * Input 
     **/
    public type: "email" | "named-resource" | "security-group";
    public actionableKind: "email";
    public displayLabel: string;
    public ngModel: string[];

    public emailAllLabel = useCmsHere_EmailAllLabel;

    public resources: Resource[] = [];
    public progressMonitoredOperationName: string;

    constructor(
        private readonly graphData: IGraphDataService,
        private readonly $q: ng.IQService,
        private readonly $window: ng.IWindowService,
        private readonly monitoredOperation: MeePortal.OneUI.Angular.IMonitoredOperation,
        private readonly meeUtil: MeePortal.OneUI.Angular.IMeeUtil) {
    }

    public $onInit(): void {
        this.progressMonitoredOperationName = `initializeResources${this.meeUtil.nextUid()}`;

        this.initializeResourcesWithProgress();
    }

    public $onChanges(changes: ng.IOnChangesObject): void {
        // NOTE: If there is a need to make changes to ngModel within this component
        //       use NgModelController and $setViewValue to notify parent controller
        //       of changes
        if (changes.ngModel && !changes.ngModel.isFirstChange()) {
            this.initializeResources();
        }
    }

    public initializeResourcesWithProgress(): ng.IPromise<any> {
        return this.monitoredOperation(this.progressMonitoredOperationName, () => this.initializeResources());
    }

    private initializeResources(): ng.IPromise<any> {
        this.resources = [];
        let promises: ng.IPromise<void>[] = [];
        let graphDataMethod: (id: string) => ng.IPromise<SupportedResourceTypes>;

        switch (this.type) {
            case "email":
                graphDataMethod = resourceName => this.graphData.getContactByEmail(resourceName);
                break;
            case "named-resource":
                // Not MSGraph specific
                graphDataMethod = resourceName => this.$q.resolve({
                    id: resourceName,
                    displayName: resourceName,
                    email: "",
                    isInvalid: false
                });
                break;
            case "security-group":
                graphDataMethod = resourceName => this.graphData.getSecurityGroupById(resourceName);
                break;
            default:
                return SharedTypes.invalidConditionBreakBuild(this.type);
        }

        if (this.ngModel) {
            this.ngModel.forEach((resourceDisplayName) => {
                promises.push(graphDataMethod(resourceDisplayName)
                    .then((graphDataResponse: SupportedResourceTypes) => {
                        this.resources.push({
                            displayName: graphDataResponse.displayName,
                            email: graphDataResponse.email,
                            isInvalid: false,
                        });
                    })
                    .catch(() => {
                        // The network call will fail if the resource is not found.
                        this.resources.push({
                            displayName: resourceDisplayName,
                            email: "",
                            isInvalid: true,
                        });
                    }));
            });
        }
        return this.$q.all(promises);
    }

    public isResourceEmpty(): boolean {
        return this.resources.length === 0;
    }

    public getResourceClass(resource: Resource): SupportedResourceClass {
        if (resource.isInvalid) {
            return "invalid";
        } else {
            if (this.isResourceMailEnabled(resource)) {
                return "interaction-enabled";
            } else {
                return "interaction-disabled";
            }
        }
    }

    public getResourceTitle(resource: Resource): string {
        if (resource.isInvalid) {
            return useCmsHere_InvalidLabel;
        } else {
            if (this.isResourceMailEnabled(resource)) {
                return useCmsHere_EmailIndividualLabel;
            } else {
                return "";
            }
        }
    }

    public isResourceMailEnabled(resource: Resource): boolean {
        return (this.type === "email" && !!resource.email) || (this.type === "named-resource" && this.actionableKind === "email");
    }

    public resourceClicked(resource: Resource): void {
        if (this.isResourceMailEnabled(resource)) {
            this.sendMail(resource);
        }
    }

    public canMailAllResources(): boolean {
        if (!this.resources.length) {
            return false;
        }

        return _.any(this.resources, (resource) => this.isResourceMailEnabled(resource));
    }

    public mailAllResources(): void {
        this.sendMail(...this.resources);
    }

    public sendMail(...resources: Resource[]) {
        this.$window.location.replace(`mailto:${this.getRecipients(...resources)}`);
    }

    public getRecipients(...resources: Resource[]) {
        let recipients: string[] = _.map(resources, (resource) => {
            switch (this.type) {
                case "email":
                    return resource.email;
                case "named-resource":
                    return resource.displayName;
                case "security-group":
                default:
                    return SharedTypes.throwUnsupportedLiteralType(this.type);
            }
        });
        return recipients.join(";");
    }
}
