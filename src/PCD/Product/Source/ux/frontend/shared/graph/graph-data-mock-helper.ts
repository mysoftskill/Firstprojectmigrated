import * as GraphTypes from "./graph-types";

export class GraphDataMockHelper {

    constructor(
        private readonly $promises: ng.IQService) { }

    public createSecurityGroupsFnWithPrefix(): (prefixString: string) => ng.IPromise<GraphTypes.Group[]> {
        return (prefixString: string) => {
            let fakeGroups = _.range(3).reverse().map(idx => {
                return <GraphTypes.Group> {
                    id: `I9n_SG${idx + 1}_Id`,
                    displayName: `${prefixString}${idx + 1}_Name`,
                    email: `I9n_SG${idx + 1}_Email`,
                    isInvalid: false,
                    securityEnabled: true,
                };
            });

            return this.$promises.resolve(fakeGroups);
        };
    }

    public getSecurityGroupFnWithName(): (name: string) => GraphTypes.Group {
        return (name: string) => {
            return <GraphTypes.Group> {
                id: "I9n_SG_Id",
                displayName: name,
                email: "I9n_SG_Email",
                isInvalid: false,
                securityEnabled: true,
            };
        };
    }

    public getSecurityGroupFnById(): (id: string, lookupCache?: boolean) => ng.IPromise<GraphTypes.Group> {
        return (id: string, lookupCache?: boolean) => {
            let fakeGroup = <GraphTypes.Group> {
                id: id,
                displayName: "I9n_SG_Name",
                email: "I9n_SG_Email",
                isInvalid: false,
                securityEnabled: true,
            };

            return this.$promises.resolve(fakeGroup);
        };
    }

    public createApplicationsFnWithPrefix(): (prefixString: string) => ng.IPromise<GraphTypes.Application[]> {
        return (prefixString: string) => {
            let fakeApplications = _.range(3).reverse().map(idx => {
                return <GraphTypes.Application> {
                    id: `I9n_App${idx + 1}_Id`,
                    displayName: `${prefixString}${idx + 1}_Name`,
                    email: `I9n_App${idx + 1}_Email`,
                    isInvalid: false
                };
            });

            return this.$promises.resolve(fakeApplications);
        };
    }

    public getApplicationFnWithName(): (name: string) => GraphTypes.Application {
        return (name: string) => {
            return <GraphTypes.Application> {
                id: "I9n_App_Id",
                displayName: name,
                email: "I9n_App_Email",
                isInvalid: false,
            };
        };
    }

    public getApplicationFnById(): (id: string, lookupCache?: boolean) => ng.IPromise<GraphTypes.Application> {
        return (id: string, lookupCache?: boolean) => {
            let fakeApplication = <GraphTypes.Application> {
                id: id,
                displayName: "I9n_App_Name",
                email: "I9n_App_Email",
                isInvalid: false,
                securityEnabled: true,
            };

            return this.$promises.resolve(fakeApplication);
        };
    }

    public getContactFnById(): (id: string) => ng.IPromise<GraphTypes.Contact> {
        return (id: string) => {
            let fakeContact = <GraphTypes.Contact> {
                id: id,
                displayName: "I9n_Contact_Name",
                email: "I9n_Contact_Email",
                isInvalid: false
            };

            return this.$promises.resolve(fakeContact);
        };
    }

    public getContactFnByEmail(): (email: string) => ng.IPromise<GraphTypes.Contact> {
        return (email: string) => {
            let fakeContact = <GraphTypes.Contact> {
                id: "I9n_Contact_Id",
                displayName: "I9n_Contact_Name",
                email: email,
                isInvalid: false
            };

            return this.$promises.resolve(fakeContact);
        };
    }

    public getContactFnFromCache(): (name: string) => ng.IPromise<GraphTypes.Contact> {
        return (name: string) => {
            let fakeContact = <GraphTypes.Contact> {
                id: "I9n_Contact_Id",
                displayName: name,
                email: "I9n_Contact_Email",
                isInvalid: false
            };

            return this.$promises.resolve(fakeContact);
        };
    }
}
