import * as SelectorTypes from "../directory-resource-selector-types";
import { StringUtilities } from "../../../../shared/string-utilities";

const useCmsHere_NoSuggestionLabel = "";
const useCmsHere_PlaceholderLabel = "";
const useCmsHere_CatchAllLabel = "";

const DefaultStrings: SelectorTypes.SelectorStringsType = {
    // Strings not needed because auto-suggest is not available for this feature.
    noSuggestionLabel: useCmsHere_NoSuggestionLabel,
    placeholderLabel: useCmsHere_PlaceholderLabel,
    catchAllLabel: useCmsHere_CatchAllLabel,
};

/**
 * Implementation of named resource selector.
 */
export class NamedResourceSelector implements SelectorTypes.IResourceSelector {
    public static $inject = ["ngModel", "$q"];

    constructor(
        private ngModel: SelectorTypes.NamedResourceSelectorData,
        private readonly $q: ng.IQService
    ) { }

    public initializeResources(): ng.IPromise<SelectorTypes.Resource[]> {
        return this.$q.resolve(this.ngModel.resources.map(resource => {
            return {
                id: resource.id,
                displayName: resource.displayName,
                isInvalid: false,
            };
        }));
    }

    public getResourceId(resourceName: string): string {
        return resourceName;
    }

    public addResource(displayName: string): void {
        this.ngModel.resources.push({
            id: displayName,
            displayName: displayName
        });
    }

    public removeResource(resourceId: string): void {
        this.ngModel.resources =
            _.reject(this.ngModel.resources, (resource) => {
                return resource.id === resourceId;
            });
    }

    public isAutoSuggestAllowed(): boolean {
        return this.ngModel.isAutoSuggestAllowed;
    }

    public canAddToExistingResources(addedResources: SelectorTypes.AddedResource[], displayName: string): boolean {
        return !_.some(addedResources, (resource) => {
            return displayName === resource.displayName;
        });
    }

    public getResourcesWithPrefix(lookupText: string): ng.IPromise<SelectorTypes.DisplayData[]> {
        if (!this.isAutoSuggestAllowed() || !lookupText || !lookupText.trim()) {
            return this.$q.resolve([]);
        }

        let result = _.filter(this.ngModel.autoSuggestionList, (namedResource) => {
            return StringUtilities.containsIgnoreCase(namedResource, lookupText.trim());
        });

        return this.$q.resolve(this.transformToDisplayFormat(result));
    }

    private transformToDisplayFormat(namedResources: string[]): SelectorTypes.DisplayData[] {
        return namedResources.map(namedResource => {
            return {
                type: "string",
                value: namedResource
            };
        });
    }

    public isResourceNameValid(displayName: string): boolean {
        //Restricts the use of any other value other than a member of the autoSuggestionList
        if (this.isAutoSuggestAllowed()) {
            return _.contains(this.ngModel.autoSuggestionList, displayName);
        }
        return true;
    }

    public getDefaultStrings(): SelectorTypes.StringsGroup {
        return DefaultStrings;
    }

    public getResourceDisplayName(displayName: string): string {
        return displayName;
    }

    public getNgModel(): SelectorTypes.NamedResourceSelectorData {
        return this.ngModel;
    }
}
