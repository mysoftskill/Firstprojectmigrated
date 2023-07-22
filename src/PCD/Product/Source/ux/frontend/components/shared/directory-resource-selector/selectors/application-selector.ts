import * as SelectorTypes from "../directory-resource-selector-types";
import { IGraphDataService } from "../../../../shared/graph/graph-data.service";
import * as GraphTypes from "../../../../shared/graph/graph-types";

const useCmsHere_NoSuggestionLabel = "No applications found";
const useCmsHere_PlaceholderLabel = "Type application name or ID";
const useCmsHere_CatchAllLabel = "Could not get applications, continue typing";

const DefaultStrings: SelectorTypes.SelectorStringsType = {
    noSuggestionLabel: useCmsHere_NoSuggestionLabel,
    placeholderLabel: useCmsHere_PlaceholderLabel,
    catchAllLabel: useCmsHere_CatchAllLabel,
};

/**
 * Implementation of application selector.
 */
export class ApplicationSelector implements SelectorTypes.IResourceSelector {
    public static $inject = ["ngModel", "graphDataService", "$q"];

    constructor(
        private ngModel: SelectorTypes.ApplicationSelectorData,
        private readonly graphData: IGraphDataService,
        private readonly $q: ng.IQService
    ) { }

    // Initialize 'application' of ngModel.
    public initializeResources(): ng.IPromise<SelectorTypes.Resource[]> {
        return this.getApplicationData()
            .then((applicationArray: GraphTypes.GraphResource[]) => {
                this.ngModel.applications = [];

                (<SelectorTypes.Application[]>applicationArray).forEach((app) => {
                    this.ngModel.applications.push(app);
                });

                return applicationArray;
            });
    }

    public getResourceId(resourceName: string): string {
        let application: GraphTypes.Application = this.graphData.getApplicationFromCache(resourceName);
        return application && application.id;
    }

    public addResource(displayName: string): void {
        let application: GraphTypes.Application = this.graphData.getApplicationFromCache(displayName);

        this.ngModel.applications.push({
            id: application && application.id,
            displayName: application && application.displayName,
            isInvalid: false
        });
    }

    public removeResource(resourceId: string): void {
        this.ngModel.applications =
            _.reject(this.ngModel.applications, (app) => {
                return app.id === resourceId;
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
        return this.graphData.getApplicationsWithPrefix(lookupText)
            .then((apps) => {
                // This indicates that we got back no results. Return empty set so that
                // mwf defaults to showing 'noSuggestionsLabel'
                if (!apps || !apps.length) {
                    return [];
                } else {
                    return this.transformToDisplayFormat(apps);
                }
            })
            .catch((e) => {
                if (e.jqXHR.status === 400) {
                    // This indicates that our exact match for "id" failed with a "Bad Request".
                    return [];
                } else {
                    // This indicates that we could not fetch applications due to 500.
                    console.error("Could not retrieve applications for user. Error: " + e);
                    return [{
                        type: "string",
                        value: this.getDefaultStrings().catchAllLabel
                    }];
                }
            });
    }

    private transformToDisplayFormat(applications: GraphTypes.GraphResource[]): SelectorTypes.DisplayData[] {
        return applications.map( (app: GraphTypes.Application) => {
            let value = app.displayName;

            return {
                type: "string",
                value: value
            };
        });
    }

    public isResourceNameValid(displayName: string): boolean {
        return this.graphData.isApplicationNameValid(displayName);
    }

    public getDefaultStrings(): SelectorTypes.StringsGroup {
        return DefaultStrings;
    }

    public getResourceDisplayName(displayName: string): string {
        return this.graphData.getApplicationFromCache(displayName).displayName;
    }

    public getNgModel(): SelectorTypes.ApplicationSelectorData {
        return this.ngModel;
    }

    private getApplicationData(): ng.IPromise<GraphTypes.Application[]> {

        let promises: ng.IPromise<GraphTypes.Application>[] = [];

        this.ngModel.applications.forEach((app: SelectorTypes.Application) => {
            promises.push(this.graphData.getApplicationById(app.id)
                .then((appResource: GraphTypes.Application) => {
                    return {
                        id: appResource.id,
                        displayName: appResource.displayName,
                        isInvalid: false
                    };
                })
                .catch(() => {
                    // The network call will fail if the resource is not found.
                    return {
                        id: app.id,
                        displayName: app.id,
                        isInvalid: true
                    };
                })
            );
        });

        return this.$q.all(promises);
    }
}
