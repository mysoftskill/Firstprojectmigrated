import * as SelectorTypes from "../directory-resource-selector-types";
import { IGraphDataService } from "../../../../shared/graph/graph-data.service";
import * as GraphTypes from "../../../../shared/graph/graph-types";

const useCmsHere_NoSuggestionLabel = "No contacts found";
const useCmsHere_PlaceholderLabel = "Type contact name or email";
const useCmsHere_CatchAllLabel = "Could not get contact data, continue typing";

const DefaultStrings: SelectorTypes.SelectorStringsType = {
    noSuggestionLabel: useCmsHere_NoSuggestionLabel,
    placeholderLabel: useCmsHere_PlaceholderLabel,
    catchAllLabel: useCmsHere_CatchAllLabel,
};

/**
 * Implementation of Contact Selector.
 */
export class ContactSelector implements SelectorTypes.IResourceSelector {
    public static $inject = ["ngModel", "graphDataService", "$q"];

    constructor(
        private ngModel: SelectorTypes.ContactSelectorData,
        private graphData: IGraphDataService,
        private readonly $q: ng.IQService
    ) { }

    // Initialize 'contacts' of this.ngModel.
    public initializeResources(): ng.IPromise<SelectorTypes.Resource[]> {
        return this.getContactData()
            .then((contactArray: GraphTypes.Contact[]) => {
                this.ngModel.contacts = [];

                contactArray.forEach((contact) => {
                    this.ngModel.contacts.push({
                        id: contact.id,
                        email: contact.email,
                        displayName: contact.displayName,
                        isInvalid: contact.isInvalid
                    });
                });

                return contactArray;
            });
    }

    public getResourceId(displayName: string): string {
        let contact: GraphTypes.Contact = this.graphData.getContactFromCache(displayName);

        return contact && contact.id;
    }

    public addResource(displayName: string): void {
        let contact: GraphTypes.Contact = this.graphData.getContactFromCache(displayName);

        this.ngModel.contacts.push({
            id: contact && contact.id,
            displayName: contact && contact.displayName,
            email: contact && contact.email,
            isInvalid: false
        });
    }

    public getResourceDisplayName(displayName: string): string {
        // Example: If resourceName is "Jessica Hunt (jessicah@microsoft.com)", return "Jessica Hunt".
        let resourceName = this.extractResourceName(displayName);

        return this.graphData.getContactFromCache(resourceName).displayName;
    }

    public removeResource(resourceId: string): void {
        this.ngModel.contacts = _.reject(this.ngModel.contacts, (contact: SelectorTypes.Contact) => {
            return contact.id === resourceId;
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
        return this.graphData.getContactsWithPrefix(lookupText)
            .then((contacts) => {
                if (!contacts || !contacts.length) {
                    // This indicates that we got back no results. Return empty set so that
                    // mwf defaults to showing 'noSuggestionsLabel'
                    return [];
                } else {
                    return this.transformToDisplayFormat(contacts);
                }
            })
            .catch((e) => {
                console.error("Could not retrieve contacts for suggestions. Error: " + e);
                return [{
                    type: "string",
                    value: this.getDefaultStrings().catchAllLabel
                }];
            });
    }

    private transformToDisplayFormat(contacts: GraphTypes.GraphResource[]): SelectorTypes.DisplayData[] {
        return contacts.map( (contact: GraphTypes.Contact) => {
            return {
                type: "string",
                value: `${contact.displayName} (${contact.email})`
            };
        });
    }

    public isResourceNameValid(displayName: string): boolean {
        let name: string = this.extractResourceName(displayName);

        return this.graphData.isContactNameValid(name);
    }

    public getDefaultStrings(): SelectorTypes.StringsGroup {
        return DefaultStrings;
    }

    public getNgModel(): SelectorTypes.ContactSelectorData {
        return this.ngModel;
    }

    private getContactData(): ng.IPromise<GraphTypes.Contact[]> {

        let modelContacts = this.ngModel.contacts;
        let promises: ng.IPromise<GraphTypes.Contact>[] = [];

        modelContacts.forEach((contact: SelectorTypes.Contact) => {
            promises.push(this.graphData.getContactByEmail(contact.email)
                .then((contactResponse: GraphTypes.Contact) => {
                    return {
                        id: contactResponse.id,
                        displayName: contactResponse.displayName,
                        isInvalid: false,
                        email: contactResponse.email
                    };
                })
                .catch(() => {
                    // The network call will fail is the resource is not found.
                    return {
                        id: contact.id,
                        displayName: contact.email,
                        isInvalid: true,
                        email: contact.email
                    };
                })
            );
        });

        return this.$q.all(promises);
    }

    private extractResourceName(displayName: string): string {
        let lastIndexOfOpenBracket = displayName.lastIndexOf("(");
        let lastIndexForResourceName = lastIndexOfOpenBracket === -1 ? displayName.length : (lastIndexOfOpenBracket - 1);

        return displayName.substring(0, lastIndexForResourceName);
    }
}
