import * as angular from "angular";
import { Service, Inject } from "../../module/app.module";

import { IMsalTokenManagerFactory } from "../msal-token-manager";
import { IAjaxService, IAjaxServiceFactory, IAjaxServiceOptions } from "../ajax.service";
import * as GraphTypes from "./graph-types";

/**
 *  Provides access to MS Graph API. Do not use this directly, use Graph data service instead. 
 **/
export interface IGraphApiService {

    /**
     *  Gets all users that match the prefix string. 
     **/
    getAllUsersWithFilter(prefix: string, filter: UserPrefixFilter): ng.IHttpPromise<GraphTypes.GraphAjaxResponseUserData>;

    /**
     *  Gets all groups that match the prefix string. 
     **/
    getAllGroupsWithFilter(prefix: string, filter: GroupPrefixFilter): ng.IHttpPromise<GraphTypes.GraphAjaxResponseGroupData>;

    /** 
     * Gets all applications that match the prefix string. 
     **/
    getAllApplicationsWithFilter(prefix: string, filter: ApplicationPrefixFilter): ng.IHttpPromise<GraphTypes.GraphAjaxResponseApplicationData>;
}

export type GroupPrefixFilter = "displayName" | "mail" | "id";
export type UserPrefixFilter = "displayName" | "mail" | "userPrincipalName" | "id";
export type ApplicationPrefixFilter = "displayName" | "id";

const graphResourceDomain = "https://graph.microsoft.com";

@Service({
    name: "graphApiService"
})
@Inject("msalTokenManagerFactory", "ajaxServiceFactory")
class GraphApiService implements IGraphApiService {
    private ajaxService: IAjaxService;

    constructor(
        private msalTokenManagerFactory: IMsalTokenManagerFactory,
        private ajaxServiceFactory: IAjaxServiceFactory
    ) {
        let ajaxOptions: IAjaxServiceOptions = {
            authTokenManager: msalTokenManagerFactory.createInstance(graphResourceDomain)
        };
        this.ajaxService = ajaxServiceFactory.createInstance(ajaxOptions);
    }

    public getAllGroupsWithFilter(prefix: string, filter: GroupPrefixFilter): ng.IHttpPromise<GraphTypes.GraphAjaxResponseGroupData> {
        let selectorStr = this.getSelectorString(prefix, filter);

        return this.ajaxService.get({
            url: `${graphResourceDomain}/v1.0/groups?$filter=${selectorStr}`,
            serviceName: "MsGraphService",
            operationName: "getAllGroupsWithFilter"
        });
    }

    public getAllUsersWithFilter(prefix: string, filter: UserPrefixFilter): ng.IHttpPromise<GraphTypes.GraphAjaxResponseUserData> {
        let selectorStr = this.getSelectorString(prefix, filter);

        return this.ajaxService.get({
            url: `${graphResourceDomain}/v1.0/users?$filter=${selectorStr}`,
            serviceName: "MsGraphService",
            operationName: "getAllUsersWithFilter"
        });
    }

    public getAllApplicationsWithFilter(prefix: string, filter: ApplicationPrefixFilter): ng.IHttpPromise<GraphTypes.GraphAjaxResponseApplicationData> {
        let selectorStr = this.getSelectorString(prefix, filter);

        // TODO: Task 19179129 Update url to v1.0 when applications is no longer in beta.
        return this.ajaxService.get({
            url: `${graphResourceDomain}/beta/applications?$filter=${selectorStr}`,
            serviceName: "MsGraphService",
            operationName: "getAllApplicationsWithFilter"
        });
    }

    private getSelectorString(prefix: string, filter: GroupPrefixFilter | UserPrefixFilter): string {
        let selectorString = "";

        switch (filter) {
            case "displayName":
            case "mail":
            case "userPrincipalName":
                selectorString = `startswith(${filter}, '${prefix}')`;
                break;

            case "id":
                selectorString = `${filter} eq '${prefix}'`;
                break;

            default:
                selectorString = `startswith(${filter}, '${prefix}')`;
        }

        return encodeURIComponent(selectorString);
    }
}
