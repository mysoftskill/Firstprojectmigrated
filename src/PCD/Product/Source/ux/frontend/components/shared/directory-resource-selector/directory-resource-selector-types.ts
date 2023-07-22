/** 
 * Represents Resource. 
 * */
export interface Resource {
    /** 
     * ID. 
     * */
    id: string;

    /** 
     * Display name. 
     * */
    displayName: string;

    /** 
     * Is this resource invalid as determined by corresponding IResourceSelector 
     * */
    isInvalid: boolean;
}

/** Interface for Resource Selector implementation.
    This needs to be implemented by any custom implementation of directory resource selector.
*/
export interface IResourceSelector {
    initializeResources(): ng.IPromise<Resource[]>;

    addResource(resourceName: string): void;

    removeResource(resourceId: string): void;

    isAutoSuggestAllowed(): boolean;

    canAddToExistingResources(addedResources: AddedResource[], displayName: string): boolean;

    getResourcesWithPrefix(lookupText: string): ng.IPromise<DisplayData[]>;

    getResourceId(resourceName: string): string;

    isResourceNameValid(resourceName: string): boolean;

    getResourceDisplayName(resourceName: string): string;

    getNgModel(): DirectoryResourceSelectorExposedData;

    getDefaultStrings(): StringsGroup;
}

export interface DirectoryResourceSelectorExposedData { }

export interface ResourceEntity {
    id: string;

    displayName: string;
}

export interface GraphResourceEntity extends ResourceEntity {
    /** 
     * Is resource invalid, i.e. it is known to Pdms but not Graph. 
     * */
    isInvalid?: boolean;
}

export interface NamedResourceSelectorData extends DirectoryResourceSelectorExposedData {
    resources: ResourceEntity[];
    isAutoSuggestAllowed: boolean;
    autoSuggestionList: string[];
}

export interface SecurityGroupSelectorData extends DirectoryResourceSelectorExposedData {
    /** 
     * Selected security groups 
     * */
    securityGroups: SecurityGroup[];
}

export interface ApplicationSelectorData extends DirectoryResourceSelectorExposedData {
    /** 
     * Selected applications 
     * */
    applications: Application[];
}

export interface ContactSelectorData extends DirectoryResourceSelectorExposedData {
    /** 
     * Selected Contacts 
     * */
    contacts: Contact[];
}

export interface VariantSelectorData extends DirectoryResourceSelectorExposedData {
    /** 
     * Selected Variants 
     * */
    variants: VariantResource[];
}

export interface SecurityGroup extends GraphResourceEntity {
    email: string;
}

export interface Application extends GraphResourceEntity { }

export interface Contact extends GraphResourceEntity {
    email: string;
}

export interface VariantResource extends ResourceEntity { }

export interface DisplayData {
    type: string;

    value: string;
}

export interface StringsGroup {
    noSuggestionLabel: string;
    placeholderLabel: string;
    catchAllLabel: string;
}

export interface AddedResource {
    /** 
     * ID 
     **/
    id: string;

    /** 
     * Display name 
     **/
    displayName: string;

    /** 
     * Is invalid 
     **/
    isInvalid?: boolean;
}

export type SelectorStringsType = {
    noSuggestionLabel: string,
    placeholderLabel: string,
    catchAllLabel: string,
};

export type SelectorType = "security-group" | "application" | "contact" | "named-resource" | "variant";
export type SelectorTypeClassName = "security-group-selector" | "application-selector" | "contact-selector" | "named-resource-selector" | "variant-selector";
