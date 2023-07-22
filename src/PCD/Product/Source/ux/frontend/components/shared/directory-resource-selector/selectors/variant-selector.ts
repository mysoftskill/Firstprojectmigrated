import * as SelectorTypes from "../directory-resource-selector-types";
import { IVariantDataService } from "../../../../shared/variant/variant-data.service";
import { VariantDefinition } from "../../../../shared/pdms/pdms-types";

const useCmsHere_NoSuggestionLabel = "No variants found";
const useCmsHere_PlaceholderLabel = "Type variant name";
const useCmsHere_CatchAllLabel = "Could not get variant data, continue typing";

const DefaultStrings: SelectorTypes.SelectorStringsType = {
    noSuggestionLabel: useCmsHere_NoSuggestionLabel,
    placeholderLabel: useCmsHere_PlaceholderLabel,
    catchAllLabel: useCmsHere_CatchAllLabel,
};

/**
 * Implementation of Variant Selector.
 */
export class VariantSelector implements SelectorTypes.IResourceSelector {
    public static $inject = ["ngModel", "variantDataService", "$q"];

    private variantsPromise: ng.IPromise<SelectorTypes.VariantResource[]>;
    private variantsCache: SelectorTypes.VariantResource[];

    constructor(
        private ngModel: SelectorTypes.VariantSelectorData,
        private variantDataService: IVariantDataService,
        private readonly $q: ng.IQService
    ) {
        this.ngModel = {
            variants: []
        };

        // warmup call to load variants and populate cache
        this.getAllVariants();
    }

    // Initialize 'variants' of this.ngModel.
    public initializeResources(): ng.IPromise<SelectorTypes.Resource[]> {
        return this.$q.resolve(_.map(this.ngModel.variants, (variant: SelectorTypes.VariantResource) => {
            return {
                id: variant.id,
                displayName: variant.displayName,
                isInvalid: false
            };
        }));
    }

    public getResourceId(displayName: string): string {
        let variant = this.getVariantFromCache(displayName);
        return variant && variant.id;
    }

    public addResource(displayName: string): void {
        let variant = this.getVariantFromCache(displayName);

        this.ngModel.variants.push(variant);
    }

    public getResourceDisplayName(displayName: string): string {
        return displayName;
    }

    public removeResource(resourceId: string): void {
        this.ngModel.variants = _.reject(this.ngModel.variants, (variant: SelectorTypes.VariantResource) => {
            return variant.id === resourceId;
        });
    }

    public isAutoSuggestAllowed(): boolean {
        return true;
    }

    public canAddToExistingResources(addedResources: SelectorTypes.AddedResource[], displayName: string): boolean {
        // Allow adding to existing resource if resource does not already exist.
        return !_.find(addedResources, (resource) => displayName === resource.displayName);
    }

    public getResourcesWithPrefix(lookupText: string): ng.IPromise<SelectorTypes.DisplayData[]> {
        return this.getAllVariants()
            .then((variants: SelectorTypes.VariantResource[]) => {
                return this.transformToDisplayFormat(
                    _.filter(variants, v => v.displayName.toLocaleLowerCase().indexOf(lookupText.toLocaleLowerCase()) >= 0)
                );
            })
            .catch((e) => {
                return [{
                    type: "string",
                    value: this.getDefaultStrings().catchAllLabel
                }];
            });
    }

    public isResourceNameValid(displayName: string): boolean {
        return !!this.getVariantFromCache(displayName);
    }

    public getDefaultStrings(): SelectorTypes.StringsGroup {
        return DefaultStrings;
    }

    //  This is used for test purposes only. It is exposed because `ngModel` is reset in the
    //  `initializeResources` function for this selector type.
    public setNgModel(model: SelectorTypes.VariantSelectorData): void {
        this.ngModel = model;
    }

    public getNgModel(): SelectorTypes.VariantSelectorData {
        return this.ngModel;
    }

    private getAllVariants(): ng.IPromise<SelectorTypes.VariantResource[]> {
        if (this.variantsPromise) {
            return this.variantsPromise;
        }

        return this.variantsPromise = this.variantDataService.getVariants()
            .then((variants: VariantDefinition[]) => {
                return this.variantsCache = variants.map(v => {
                    return <SelectorTypes.VariantResource>{
                        id: v.id,
                        displayName: v.name
                    };
                });
            })
            .catch(() => {
                this.variantsPromise = null;
                return this.$q.reject();
            });
    }

    private getVariantFromCache(variantName: string): SelectorTypes.VariantResource {
        return _.find(this.variantsCache, (v) => v.displayName.trim() === variantName.trim());
    }

    private transformToDisplayFormat(variants: SelectorTypes.VariantResource[]): SelectorTypes.DisplayData[] {
        return variants.map( (variant: SelectorTypes.VariantResource) => {
            return {
                type: "string",
                value: variant.displayName
            };
        });
    }
}
