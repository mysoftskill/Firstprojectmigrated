import * as SelectorTypes from "../directory-resource-selector-types";
import { IGraphDataService } from "../../../../shared/graph/graph-data.service";
import * as GraphTypes from "../../../../shared/graph/graph-types";

const useCmsHere_NoSuggestionLabel = "No security groups found";
const useCmsHere_PlaceholderLabel = "Type security group name, email or ID";
const useCmsHere_CatchAllLabel = "Could not get security groups, continue typing";

const DefaultStrings: SelectorTypes.SelectorStringsType = {
    noSuggestionLabel: useCmsHere_NoSuggestionLabel,
    placeholderLabel: useCmsHere_PlaceholderLabel,
    catchAllLabel: useCmsHere_CatchAllLabel,
};

/**
 * Implementation of security group selector.
 */
export class SecurityGroupSelector implements SelectorTypes.IResourceSelector {
    public static $inject = ["ngModel", "graphDataService", "$q"];

    constructor(
        private ngModel: SelectorTypes.SecurityGroupSelectorData,
        private readonly graphData: IGraphDataService,
        private readonly $q: ng.IQService
    ) { }

    // Initialize 'securityGroups' of ngModel.
    public initializeResources(): ng.IPromise<SelectorTypes.Resource[]> {
        return this.getSecurityGroupData()
            .then((groupArray: GraphTypes.GraphResource[]) => {
                this.ngModel.securityGroups = [];

                (<SelectorTypes.SecurityGroup[]>groupArray).forEach((group) => {
                    this.ngModel.securityGroups.push(group);
                });

                return groupArray;
            });
    }

    public getResourceId(resourceName: string): string {
        let securityGroup: GraphTypes.Group = this.graphData.getSecurityGroupFromCache(resourceName);
        return securityGroup && securityGroup.id;
    }

    public addResource(displayName: string): void {
        let securityGroup: GraphTypes.Group = this.graphData.getSecurityGroupFromCache(displayName);

        this.ngModel.securityGroups.push({
            id: securityGroup && securityGroup.id,
            displayName: securityGroup && securityGroup.displayName,
            email: securityGroup && securityGroup.email,
            isInvalid: false
        });
    }

    public removeResource(resourceId: string): void {
        this.ngModel.securityGroups =
            _.reject(this.ngModel.securityGroups, (group) => {
                return group.id === resourceId;
            });
    }

    public isAutoSuggestAllowed(): boolean {
        return true;
    }

    public canAddToExistingResources(addedResources: SelectorTypes.AddedResource[], displayName: string): boolean {
        return !_.find(addedResources, (resource) => {
            return displayName.indexOf(resource.displayName) > -1;
        });
    }

    public getResourcesWithPrefix(lookupText: string): ng.IPromise<SelectorTypes.DisplayData[]> {
        return this.graphData.getSecurityGroupsWithPrefix(lookupText)
            .then((groups) => {
                // This indicates that we got back no results. Return empty set so that
                // mwf defaults to showing 'noSuggestionsLabel'
                if (!groups || !groups.length) {
                    return [];
                } else {
                    return this.transformToDisplayFormat(groups);
                }
            })
            .catch((e) => {
                if (e.jqXHR.status === 400) {
                    // This indicates that our exact match for "id" failed with a "Bad Request".
                    return [];
                } else {
                    // This indicates that we could not fetch security groups due to 500.
                    console.error("Could not retrieve groups for user. Error: " + e);
                    return [{
                        type: "string",
                        value: this.getDefaultStrings().catchAllLabel
                    }];
                }
            });
    }

    private transformToDisplayFormat(securityGroups: GraphTypes.GraphResource[]): SelectorTypes.DisplayData[] {
        return securityGroups.map( (group: GraphTypes.Group) => {
            let value = group.displayName;

            if (group.email) {
                value = `${group.displayName} (${group.email})`;
            }

            return {
                type: "string",
                value: value
            };
        });
    }

    public isResourceNameValid(displayName: string): boolean {
        return this.graphData.isSecurityGroupNameValid(this.extractResourceName(displayName));
    }

    public getDefaultStrings(): SelectorTypes.StringsGroup {
        return DefaultStrings;
    }

    public getResourceDisplayName(displayName: string): string {
        // Example: If resourceName is "MEE Device (mee-device@microsoft.com)", return "MEE Device".
        let resourceName = this.extractResourceName(displayName);

        return this.graphData.getSecurityGroupFromCache(resourceName).displayName;
    }

    public getNgModel(): SelectorTypes.SecurityGroupSelectorData {
        return this.ngModel;
    }

    private extractResourceName(displayName: string): string {
        let lastIndexOfOpenBracket = displayName.lastIndexOf("(");
        let lastIndexForResourceName = lastIndexOfOpenBracket === -1 ? displayName.length : (lastIndexOfOpenBracket - 1);

        return displayName.substring(0, lastIndexForResourceName);
    }

    private getSecurityGroupData(): ng.IPromise<GraphTypes.Group[]> {

        let promises: ng.IPromise<GraphTypes.Group>[] = [];

        this.ngModel.securityGroups.forEach((group: SelectorTypes.SecurityGroup) => {
            promises.push(this.graphData.getSecurityGroupById(group.id)
                .then((groupResponse: GraphTypes.Group) => {
                    return {
                        id: groupResponse.id,
                        displayName: groupResponse.displayName,
                        isInvalid: false,
                        email: groupResponse.email,
                        securityEnabled: true
                    };
                })
                .catch(() => {
                    // The network call will fail if the resource is not found.
                    return {
                        id: group.id,
                        displayName: group.id,
                        isInvalid: true,
                        email: group.email,
                        securityEnabled: true
                    };
                })
            );
        });

        return this.$q.all(promises);
    }
}
