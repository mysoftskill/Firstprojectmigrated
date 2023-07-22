import * as msal from "@azure/msal-browser";
import { Mode, AppConfig } from "../module/data.module";
import { appModule } from "../module/app.module";

export interface IMsalTokenManagerFactory {
    //  Method to return a MsalTokenManager object.
    createInstance: (resource: string) => IMsalTokenManager;
}

export interface IMsalTokenManager {
    //  Gets token from cache or iframe if expired.
    getTokenAsync(): ng.IPromise<string>;
}

export interface MsalTokenManagerOptions {
    refreshCsrfToken?: boolean;
    $q: ng.IQService;
    authCtx: msal.PublicClientApplication;
    resource: string;
}

export class MsalTokenManager implements IMsalTokenManager {
    private csrfDeferred: ng.IDeferred<void>;

    constructor(
        private readonly msalTokenManagerOptions: MsalTokenManagerOptions
    ) { }

    public getTokenAsync(): ng.IPromise<string> {
        let angularDeferred = this.msalTokenManagerOptions.$q.defer<string>();
        let scopes = [];
        if (this.msalTokenManagerOptions.resource === "https://graph.microsoft.com") {
            scopes=["profile", "openid", "email", "Application.Read.All", "Directory.Read.All", "User.Read"];
        } else {
            scopes=[`api://${this.msalTokenManagerOptions.resource}/user_impersonation`];
        }
        let request = {scopes: scopes, authority:this.msalTokenManagerOptions.authCtx.getConfiguration().auth.authority};
        
        this.msalTokenManagerOptions.authCtx.acquireTokenSilent(request).then((value) => {
            
            this.retrieveCsrfToken(value.accessToken)
                .then(() => {
                    angularDeferred.resolve(value.accessToken);
                })
                .catch(() => {
                    angularDeferred.reject();
                });
        }).catch((error) => {
                console.error(`Fetching access token for ${this.msalTokenManagerOptions.resource} failed with message: ${error}`);
                angularDeferred.reject(error);
        });

        return angularDeferred.promise;
    }

    private retrieveCsrfToken(authToken: string): ng.IPromise<void> {
        if (this.csrfDeferred) {
            //  Return result of a previously made request.
            return this.csrfDeferred.promise;
        }
        this.csrfDeferred = this.msalTokenManagerOptions.$q.defer<void>();

        if (this.msalTokenManagerOptions.refreshCsrfToken) {
            const headers: string[] = [];
            headers["Authorization"] = `Bearer ${authToken}`;

            const settings: Bradbury.JQueryTelemetryAjaxSettings = {
                url: "/api/getcsrftoken",
                serviceName: "Pcd",
                operationName: "GetCsrfToken",
                additionalHeaders: headers,
                requestedWithHeaderBehavior: "header"
            };

            window.BradburyTelemetry.ajax.ajaxGet(settings).then((data, textStatus, jqXHR) => {
                //  Set CSRF token value using data coming from an API call.
                if (!document.getElementsByName("__RequestVerificationToken").length) {
                    const tokenElement = document.createElement("input");
                    tokenElement.name = "__RequestVerificationToken";
                    tokenElement.type = "hidden";
                    tokenElement.value = data.token;

                    document.body.appendChild(tokenElement);
                }

                this.csrfDeferred.resolve();
            }, (jqXHR, textStatus, errorThrown) => {
                this.csrfDeferred.reject({ jqXHR, textStatus, errorThrown });
            });
        } else {
            this.csrfDeferred.resolve();
        }

        return this.csrfDeferred.promise;
    }
}

export type RegisterMsalTokenManagerFactoryRealOptions = {
    kind: "real";
    authContext: msal.PublicClientApplication;
};

export type RegisterMsalTokenManagerFactoryMockOptions = {
    kind: "mock";
    createMockInstance: ($q: ng.IQService, resource: string) => IMsalTokenManager;
};

export function registerMsalTokenManagerFactory(factoryOptions: RegisterMsalTokenManagerFactoryRealOptions | RegisterMsalTokenManagerFactoryMockOptions): void {
    const msalTokenManagerInstances: {
        [key: string]: IMsalTokenManager;
    } = {};

    appModule.factory("msalTokenManagerFactory", ["$q", "appConfig", ($q: ng.IQService, appConfig: AppConfig) => {
        return {
            createInstance: (resource: string): IMsalTokenManager => {
                if (factoryOptions.kind === "real") {
                    if (msalTokenManagerInstances[resource]) {
                        return msalTokenManagerInstances[resource];
                    }

                    let msalTokenManagerOptions: MsalTokenManagerOptions = {
                        refreshCsrfToken: resource === appConfig.azureAdAppId,
                        $q,
                        authCtx: factoryOptions.authContext,
                        resource
                    };
                    msalTokenManagerInstances[resource] = new MsalTokenManager(msalTokenManagerOptions);

                    return msalTokenManagerInstances[resource];
                } else {
                    return factoryOptions.createMockInstance($q, resource);
                }
            }
        };
    }]);
}
