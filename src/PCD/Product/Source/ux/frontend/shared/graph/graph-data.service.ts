import { Service, Inject } from "../../module/app.module";

import { IGraphApiService } from "./graph-api.service";
import * as GraphTypes from "./graph-types";
import { StringUtilities } from "../string-utilities";

export interface IGraphDataService {

    //  Gets all security groups with prefix string match.
    getSecurityGroupsWithPrefix(prefixString: string): ng.IPromise<GraphTypes.Group[]>;

    //  Find a security group by id
    getSecurityGroupById(id: string): ng.IPromise<GraphTypes.Group>;

    //  Returns true if the group name is valid.
    isSecurityGroupNameValid(name: string): boolean;

    //  Returns security group from cache.
    getSecurityGroupFromCache(name: string): GraphTypes.Group;

    //  Gets all users with prefix string match.
    getContactsWithPrefix(prefixString: string): ng.IPromise<GraphTypes.Contact[]>;

    //  Gets a user by id
    getContactById(id: string): ng.IPromise<GraphTypes.Contact>;

    //  Gets a user by email
    getContactByEmail(email: string): ng.IPromise<GraphTypes.Contact>;

    //  Gets user id from name
    getContactFromCache(name: string): GraphTypes.Contact;

    //  Returns true if the user name is valid.
    isContactNameValid(name: string): boolean;

    //  Gets all applications with prefix string match.
    getApplicationsWithPrefix(prefixString: string): ng.IPromise<GraphTypes.Application[]>;

    //  Find an application by id
    getApplicationById(id: string): ng.IPromise<GraphTypes.Application>;

    //  Returns true if the application name is valid.
    isApplicationNameValid(name: string): boolean;

    //  Returns application from cache.
    getApplicationFromCache(name: string): GraphTypes.Application;
}

@Service({
    name: "graphDataService"
})
@Inject("$q", "graphApiService")
class GraphDataCacheService implements IGraphDataService {
    private filteredSecurityGroups: GraphTypes.Group[];
    private filteredContacts: GraphTypes.Contact[];
    private filteredApplications: GraphTypes.Application[];
    private prevPrefixString: string;

    constructor(
        private $q: ng.IQService,
        private graphApiService: IGraphApiService) { }

    public getApplicationsWithPrefix(prefixString: string): ng.IPromise<GraphTypes.Application[]> {
        if (!prefixString || !prefixString.trim()) {
            return this.$q.resolve([]);
        }

        // UI responsiveness optimization through Predictive Caching- get the filtered list of applications 
        // from cached data if previous lookup string is a substring of current lookup string. 
        // This needs to happen only for strings of length greater than 3, 
        // so that we have an authoritative cache set.
        if (StringUtilities.containsIgnoreCase(prefixString, this.prevPrefixString) &&
            this.prevPrefixString.length > 6 &&
            this.filteredApplications &&
            this.filteredApplications.length) {

            this.prevPrefixString = prefixString;
            return this.$q.resolve(this.getCachedApplicationsWithPrefix(prefixString));
        }

        this.prevPrefixString = prefixString;

        // Find the applications with name filter
        return this.graphApiService.getAllApplicationsWithFilter(prefixString, "displayName")
            .then((applicationsByName: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseApplicationData>) => {
                if (this.isInvalidResponse(applicationsByName)) {

                    // Find the applicaitons with id filter
                    return this.graphApiService.getAllApplicationsWithFilter(prefixString, "id")
                        .then((applicationsById: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseApplicationData>) => {
                            if (this.isInvalidResponse(applicationsById)) {

                                console.warn("Did not receive valid data for applications.");
                                this.filteredApplications = [];
                            } else {
                                this.filteredApplications = this.extractApplications(applicationsById.data.value);
                            }

                            return this.filteredApplications;
                        });
                } else {

                    this.filteredApplications = this.extractApplications(applicationsByName.data.value);
                    return this.filteredApplications;
                }
            });
    }

    public getApplicationById(id: string): ng.IPromise<GraphTypes.Application> {

        return this.graphApiService.getAllApplicationsWithFilter(id, "id")
            .then((applicationsById: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseApplicationData>) => {
                if (this.isInvalidResponse(applicationsById)) {
                    console.warn("Did not receive valid data for applications.");
                    this.$q.reject();
                } else {

                    let applications = this.extractApplications(applicationsById.data.value);
                    return applications && applications[0];
                }
            });
    }

    public isApplicationNameValid(name: string): boolean {
        return !!this.getApplicationFromCache(name);
    }

    public getApplicationFromCache(name: string): GraphTypes.Application {
        return _.find(this.filteredApplications, (application) => {
            return name === application.displayName;
        });
    }

    public isSecurityGroupNameValid(name: string): boolean {
        return !!this.getSecurityGroupFromCache(name);
    }

    public getSecurityGroupFromCache(name: string): GraphTypes.Group {
        return _.find(this.filteredSecurityGroups, (group) => {
            return name === group.displayName;
        });
    }

    public getContactById(id: string): ng.IPromise<GraphTypes.Contact> {
        return <ng.IPromise<GraphTypes.Contact>> this.graphApiService.getAllUsersWithFilter(id, "id")
            .then((response: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseUserData>) => {
                if (this.isInvalidResponse(response)) {

                    // Pass back an empty response so that queued promises know.
                    return this.$q.resolve([]);
                } else {

                    // Transform user data and resolve.
                    this.filteredContacts = this.extractContactsFromUserData(response.data.value);
                    return this.$q.resolve(this.filteredContacts);
                }
            });
    }

    public getContactByEmail(email: string): ng.IPromise<GraphTypes.Contact> {
        return this.getContactsWithEmailFilter(email)
            .then((contactsListByEmail: GraphTypes.Contact[]) => {
                if (!contactsListByEmail || !contactsListByEmail.length) {

                    return this.$q.reject();
                } else {

                    // Return the first result found.
                    return this.$q.resolve(contactsListByEmail[0]);
                }
            });
    }

    public getContactsWithPrefix(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        if (!prefixString || !prefixString.trim()) {
            return this.$q.resolve([]);
        }

        // UI responsiveness optimization through Predictive Caching
        if (StringUtilities.containsIgnoreCase(prefixString, this.prevPrefixString) &&
            this.prevPrefixString.length > 9 &&
            this.filteredContacts &&
            this.filteredContacts.length) {

            this.prevPrefixString = prefixString;
            return this.$q.resolve(this.getCachedContactsWithPrefix(prefixString));
        }

        this.prevPrefixString = prefixString;

        // First check with all name filters. 
        // We need to batch the network requests instead of being 'greedy'. The results are unionized to form a single response list. 
        // This is to avoid scenarios where name of a person prefix-matches with the name of a group (For example "MEE Device"). 
        // The reason we are not batching *all* the calls together (just batching by category) is to avoid high latency on
        // auto suggest responsiveness. This is an optimization. 
        return this.getContactsWithNameFilter(prefixString)
            .then((contactsListByName: GraphTypes.Contact[]) => {
                if (!contactsListByName || !contactsListByName.length) {

                    // If no results, fallback to searching based on all email filters.
                    return this.getContactsWithEmailFilter(prefixString)
                        .then((contactsListByEmail: GraphTypes.Contact[]) => {
                            if (!contactsListByEmail || !contactsListByEmail.length) {

                                // Finally, keep calm and give up.                        
                                console.warn("Did not receive valid data for contacts.");
                                this.filteredContacts = [];
                                return this.$q.resolve(this.filteredContacts);
                            } else {

                                // Transform user data and resolve.
                                this.filteredContacts = contactsListByEmail;
                                return this.$q.resolve(this.filteredContacts);
                            }
                        });

                } else {

                    // Transform user data and resolve.
                    this.filteredContacts = contactsListByName;
                    return this.$q.resolve(this.filteredContacts);
                }
            });
    }

    public getContactFromCache(name: string): GraphTypes.Contact {
        return _.find(this.filteredContacts, (filteredContact) => {
            return StringUtilities.areEqualIgnoreCase(filteredContact.displayName.trim(), name.trim());
        });
    }

    public isContactNameValid(name: string): boolean {
        return !!this.getContactFromCache(name);
    }

    public getSecurityGroupsWithPrefix(prefixString: string): ng.IPromise<GraphTypes.Group[]> {
        if (!prefixString || !prefixString.trim()) {
            return this.$q.resolve([]);
        }

        // UI responsiveness optimization through Predictive Caching- get the filtered list of security groups 
        // from cached data if previous lookup string is a substring of current lookup string. 
        // This needs to happen only for strings of length greater than 3, 
        // so that we have an authoritative cache set.
        if (StringUtilities.containsIgnoreCase(prefixString, this.prevPrefixString) &&
            this.prevPrefixString.length > 6 &&
            this.filteredSecurityGroups &&
            this.filteredSecurityGroups.length) {

            this.prevPrefixString = prefixString;
            return this.$q.resolve(this.getCachedSecurityGroupsWithPrefix(prefixString));
        }

        this.prevPrefixString = prefixString;

        // Find the groups with name filter
        return this.graphApiService.getAllGroupsWithFilter(prefixString, "displayName")
            .then((groupsByName: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseGroupData>) => {
                if (this.isInvalidResponse(groupsByName)) {

                    // Find the groups with email filter
                    return this.graphApiService.getAllGroupsWithFilter(prefixString, "mail")
                        .then((groupsByEmail: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseGroupData>) => {
                            if (this.isInvalidResponse(groupsByEmail)) {

                                // Find the groups with id filter
                                return this.graphApiService.getAllGroupsWithFilter(prefixString, "id")
                                    .then((groupsById: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseGroupData>) => {
                                        if (this.isInvalidResponse(groupsById)) {

                                            console.warn("Did not receive valid data for groups.");
                                            this.filteredSecurityGroups = [];
                                        } else {
                                            this.filteredSecurityGroups = this.extractSecurityGroups(groupsById.data.value);
                                        }

                                        return this.filteredSecurityGroups;
                                    });
                            } else {

                                this.filteredSecurityGroups = this.extractSecurityGroups(groupsByEmail.data.value);
                                return this.filteredSecurityGroups;
                            }
                        });
                } else {

                    this.filteredSecurityGroups = this.extractSecurityGroups(groupsByName.data.value);
                    return this.filteredSecurityGroups;
                }
            });
    }

    public getSecurityGroupById(id: string): ng.IPromise<GraphTypes.Group> {

        return this.graphApiService.getAllGroupsWithFilter(id, "id")
            .then((groupsById: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseGroupData>) => {
                if (this.isInvalidResponse(groupsById)) {
                    console.warn("Did not receive valid data for groups.");
                    this.$q.reject();
                } else {

                    let securityGroups = this.extractSecurityGroups(groupsById.data.value);
                    return securityGroups && securityGroups[0];
                }
            });
    }

    private getContactsWithNameFilter(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        let nameFilterPromises: ng.IPromise<GraphTypes.Contact[]>[] = [];

        nameFilterPromises.push(this.getContactsWithUserNameFilter(prefixString),
                                this.getContactsWithGroupNameFilter(prefixString));

        return this.$q.all(nameFilterPromises)
            .then((contactsListByName: GraphTypes.Contact[][]) => {
                if (!contactsListByName || (!contactsListByName[0].length && !contactsListByName[1].length)) {
                    return this.$q.resolve([]);
                } else {
                    return this.$q.resolve(_.union(contactsListByName[0], contactsListByName[1]));
                }
            });
    }

    private getContactsWithEmailFilter(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        let emailFilterPromises: ng.IPromise<GraphTypes.Contact[]>[] = [];

        emailFilterPromises.push(this.getContactsWithUserEmailFilter(prefixString),
                                this.getContactsWithGroupEmailFilter(prefixString));

        return this.$q.all(emailFilterPromises)
            .then((contactsListByEmail: GraphTypes.Contact[][]) => {
                if (!contactsListByEmail || (!contactsListByEmail[0].length && !contactsListByEmail[1].length)) {
                    return this.$q.resolve([]);
                } else {
                    return this.$q.resolve(_.union(contactsListByEmail[0], contactsListByEmail[1]));
                }
            });
    }

    private getContactsWithUserNameFilter(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        return this.graphApiService.getAllUsersWithFilter(prefixString, "displayName")
            .then((response: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseUserData>) => {
                if (this.isInvalidResponse(response)) {

                    // Pass back an empty response so that queued promises know.
                    return this.$q.resolve([]);
                } else {

                    // Transform user data and resolve.
                    this.filteredContacts = this.extractContactsFromUserData(response.data.value);
                    return this.$q.resolve(this.filteredContacts);
                }
            });
    }

    private getContactsWithGroupNameFilter(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        return this.graphApiService.getAllGroupsWithFilter(prefixString, "displayName")
            .then((groupsByDisplayName: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseGroupData>) => {
                if (this.isInvalidResponse(groupsByDisplayName)) {

                    // Pass back an empty response so that queued promises know.
                    return this.$q.resolve([]);
                } else {

                    // Transform group data and resolve.
                    this.filteredContacts = this.extractContactsFromGroupData(groupsByDisplayName.data.value);
                    return this.$q.resolve(this.filteredContacts);
                }
            });
    }

    private getContactsWithUserEmailFilter(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        /** We need to filter with 'userPrincipalName' and 'mail' property. Graph populates 'userPrincipalName'
        * by the real email id of the user, while 'mail' property with the friendly email alias.
        * For example, for a user Jane Doe, Graph data structure looks like:
        *
        * displayName: "Jane Doe"
        * mail: Jane.Doe@microsoft.com
        * userPrincipalName: jdoe@microsoft.com
        */
        let userEmailFilterPromises: ng.IPromise<GraphTypes.Contact[]>[] = [];

        userEmailFilterPromises.push(this.getContactsWithUserEmailFilterUsingPrincipalName(prefixString),
                                     this.getContactsWithUserEmailFilterUsingMail(prefixString));

        return this.$q.all(userEmailFilterPromises)
            .then((contactsListByEmail: GraphTypes.Contact[][]) => {
                if (!contactsListByEmail || (!contactsListByEmail[0].length && !contactsListByEmail[1].length)) {
                    return this.$q.resolve([]);
                } else {
                    return this.$q.resolve(_.uniq(_.union(contactsListByEmail[0], contactsListByEmail[1]), (contact: GraphTypes.Contact) => {
                        return contact.id;
                    }));
                }
            });
    }

    private getContactsWithUserEmailFilterUsingPrincipalName(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        return this.graphApiService.getAllUsersWithFilter(prefixString, "userPrincipalName")
            .then((usersByPrincipalName: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseUserData>) => {
                if (this.isInvalidResponse(usersByPrincipalName)) {

                    // Pass back an empty response so that queued promises know.
                    return this.$q.resolve([]);
                } else {

                    // Transform user data and resolve.
                    this.filteredContacts = this.extractContactsFromUserData(usersByPrincipalName.data.value);
                    return this.$q.resolve(this.filteredContacts);
                }
            });
    }

    private getContactsWithUserEmailFilterUsingMail(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        return this.graphApiService.getAllUsersWithFilter(prefixString, "mail")
            .then((usersByMail: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseUserData>) => {
                if (this.isInvalidResponse(usersByMail)) {

                    // Pass back an empty response so that queued promises know.
                    return this.$q.resolve([]);
                } else {

                    // Transform user data and resolve.
                    this.filteredContacts = this.extractContactsFromUserData(usersByMail.data.value);
                    return this.$q.resolve(this.filteredContacts);
                }
            });
    }

    private getContactsWithGroupEmailFilter(prefixString: string): ng.IPromise<GraphTypes.Contact[]> {
        return this.graphApiService.getAllGroupsWithFilter(prefixString, "mail")
            .then((groupsByMail: ng.IHttpPromiseCallbackArg<GraphTypes.GraphAjaxResponseGroupData>) => {
                if (this.isInvalidResponse(groupsByMail)) {

                    // Pass back an empty response so that queued promises know.
                    return this.$q.resolve([]);
                } else {

                    // Transform group data and resolve.
                    this.filteredContacts = this.extractContactsFromGroupData(groupsByMail.data.value);
                    return this.$q.resolve(this.filteredContacts);
                }
            });
    }

    private getCachedSecurityGroupsWithPrefix(prefixString: string): GraphTypes.Group[] {
        if (!this.filteredSecurityGroups || !this.filteredSecurityGroups.length) {
            // This should happen in very rare cases. In this case, just return an empty list. 
            // But attempt to prime the cache anyway.
            this.getSecurityGroupsWithPrefix(prefixString);
            return [];
        }

        prefixString = prefixString.trim();
        let filteredSecurityGroups: GraphTypes.Group[] = this.filteredSecurityGroups.filter((group: GraphTypes.Group) => {
            let displayName = group.displayName && group.displayName.trim();

            // See if any group's display name matches prefix string.
            return displayName ? StringUtilities.containsIgnoreCase(displayName, prefixString) : false;
        });

        if (!filteredSecurityGroups || !filteredSecurityGroups.length) {
            // See if any groups's id matches prefix string.
            filteredSecurityGroups = this.filteredSecurityGroups.filter((group: GraphTypes.Group) => {
                return group.id ? StringUtilities.containsIgnoreCase(group.id, prefixString) : false;
            });
        }

        this.filteredSecurityGroups = filteredSecurityGroups || [];
        return this.filteredSecurityGroups;
    }

    private getCachedContactsWithPrefix(prefixString: string): GraphTypes.Contact[] {
        if (!this.filteredContacts || !this.filteredContacts.length) {
            this.getContactsWithPrefix(prefixString);
            return [];
        }

        prefixString = prefixString.trim();
        let filteredContacts: GraphTypes.Contact[] = this.filteredContacts.filter((user: GraphTypes.Contact) => {
            let displayName = user.displayName && user.displayName.trim();

            // See if any user's display name matches prefix string.
            return displayName ? StringUtilities.containsIgnoreCase(displayName, prefixString) : false;
        });

        if (!filteredContacts || !filteredContacts.length) {
            // See if any contact's email matches prefix string.
            filteredContacts = this.filteredContacts.filter((contact: GraphTypes.Contact) => {
                let email = contact.email && contact.email.trim();

                return email ? StringUtilities.containsIgnoreCase(email, prefixString) : false;
            });
        }

        this.filteredContacts = filteredContacts || [];
        return this.filteredContacts;
    }

    private getCachedApplicationsWithPrefix(prefixString: string): GraphTypes.Application[] {
        if (!this.filteredApplications || !this.filteredApplications.length) {
            // This should happen in very rare cases. In this case, just return an empty list. 
            // But attempt to prime the cache anyway.
            this.getApplicationsWithPrefix(prefixString);
            return [];
        }

        prefixString = prefixString.trim();
        let filteredApplications: GraphTypes.Application[] = this.filteredApplications.filter((app: GraphTypes.Application) => {
            let displayName = app.displayName && app.displayName.trim();

            // See if any application's display name matches prefix string.
            return displayName ? StringUtilities.containsIgnoreCase(displayName, prefixString) : false;
        });

        if (!filteredApplications || !filteredApplications.length) {
            // See if any application's id matches prefix string.
            filteredApplications = this.filteredApplications.filter((app: GraphTypes.Application) => {
                return app.id ? StringUtilities.containsIgnoreCase(app.id, prefixString) : false;
            });
        }

        this.filteredApplications = filteredApplications || [];
        return this.filteredApplications;
    }

    private extractContactsFromUserData(users: GraphTypes.GraphUser[]): GraphTypes.Contact[] {
        if (!users || !users.length) {
            return;
        }

        return users.map((user: GraphTypes.GraphUser) => {
                return {
                    id: user.id,
                    displayName: user.displayName,
                    email: user.userPrincipalName || user.mail,
                    isInvalid: false
                };
            });
    }

    private extractContactsFromGroupData(groups: GraphTypes.GraphGroup[]): GraphTypes.Contact[] {
        if (!groups || !groups.length) {
            return;
        }

        // We need to search within Distribution Groups and mail enabled Security Groups.
        return _.reject(groups, (group: GraphTypes.GraphGroup) => {
                // Reject the Security Groups which have mail disabled. 
                return group.securityEnabled && !group.mailEnabled;
            })
            .map((user: GraphTypes.GraphGroup) => {
                return {
                    id: user.id,
                    displayName: user.displayName,
                    email: user.mail,
                    isInvalid: false
                };
            });
    }

    private extractSecurityGroups(groups: any[]): GraphTypes.Group[] {
        if (!groups || !groups.length) {
            return;
        }

        let securityGroups: any[] = groups.filter((group: GraphTypes.Group) => {
            return !!group.securityEnabled;
        });

        return securityGroups.map((group: GraphTypes.GraphGroup) => {
            return {
                id: group.id,
                displayName: group.displayName,
                securityEnabled: group.securityEnabled,
                email: group.mail,
                isInvalid: false
            };
        });
    }

    private extractApplications(apps: any[]): GraphTypes.Application[] {
        if (!apps || !apps.length) {
            return;
        }

        return apps.map((app: GraphTypes.Application) => {
            return {
                id: app.id,
                displayName: app.displayName,
                isInvalid: false
            };
        });
    }

    private isInvalidResponse(response: any): boolean {
        response = <GraphTypes.GraphAjaxResponseGroupData | GraphTypes.GraphAjaxResponseUserData> response;

        return !response || !response.data || !response.data.value || !response.data.value.length;
    }
}
