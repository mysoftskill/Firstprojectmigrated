import * as angular from "angular";
import * as msal from "@azure/msal-browser";

import { appModule } from "./module/app.module";
import { dataModule, AppConfig, LockdownOptions, Mode } from "./module/data.module";
import { IMsalTokenManager, IMsalTokenManagerFactory, MsalTokenManager, MsalTokenManagerOptions, registerMsalTokenManagerFactory } from "./shared/msal-token-manager";

import "./module/app.bootstrap";
import { bootstrapTelemetry, BootstrapTelemetryResult } from "./bootstrap-telemetry";
import { initializeMeControl } from "./me-control";
import { CmsContentCollection } from "./shared/cms/cms-types";
import { registerAjaxServiceFactory } from "./shared/ajax.service";

//  Arguments for main.bootstrapApp().
export interface AppBootstrapModel {
    azureAdAppId: string;
    jsllAppId: string;
    i9nMode: boolean;
    allowMocks: boolean;
    // TODO Define proper letter casing for PPE/PROD if deployed, then use string literal type.
    environmentType: string;
    lockdown?: LockdownOptions;
    preLoadedCmsContentItems: CmsContentCollection;
}

/**
* Exposes methods for test mocking purposes.
*/
export interface IUntestableMain {
    /**
     * Following methods have been exposed for only test mocking purposes. 
     * They should not be used from outside code, except tests.
     */
    configureTelemetry(jsllAppId: string): void;
    createAuthContext(azureAdAppId: string): msal.PublicClientApplication;
    startApp(appBootstrapModel: AppBootstrapModel, authCtx: msal.PublicClientApplication): void;
}

export class Main {
    private untestableMain: UntestableMain;

    constructor() {
        this.untestableMain = new UntestableMain();
    }

    /**
     * Ensures the user is signed in, then bootstraps the application.
     * @param appBootstrapModel model passed from WebRole.
     */
    public async bootstrapApp(appBootstrapModel: AppBootstrapModel): Promise<void> {
        this.untestableMain.configureTelemetry(appBootstrapModel.jsllAppId);

        let authCtx = await this.configureAuth(appBootstrapModel);
        if (!authCtx) {
            //  Shortcircuit if we don't have the auth token yet or it could not be retrieved. 
            //  There is no point in bootstrapping the app without auth token. 
            return;
        }

        this.untestableMain.startApp(appBootstrapModel, authCtx);
    }

    /**
     * This method is exposed for only test mocking purposes. Do not use in real code.
     */
    public getUntestableMainForMocking(): UntestableMain {
        return this.untestableMain;
    }

    private async configureAuth(appBootstrapModel: AppBootstrapModel): Promise<msal.PublicClientApplication> {
        let authCtx = this.untestableMain.createAuthContext(appBootstrapModel.azureAdAppId);

        if (!appBootstrapModel.i9nMode) {
            let tokenResponse = await authCtx.handleRedirectPromise();
            if (!tokenResponse) {
                authCtx.loginRedirect();
                return;
            } else {
                authCtx.setActiveAccount(tokenResponse.account);
            }
        } else {

            console.debug("Running in integration testing mode.");
        }

        return authCtx;
    }
}

class UntestableMain implements IUntestableMain {
    public configureTelemetry(jsllAppId: string): void {
        let telemetry = bootstrapTelemetry(jsllAppId);
        appModule.service("correlationContext", () => telemetry.correlationContext);
    }

    public startApp(appBootstrapModel: AppBootstrapModel, authCtx: msal.PublicClientApplication): void {
        registerMsalTokenManagerFactory({
            kind: "real",
            authContext: authCtx
        });

        registerAjaxServiceFactory({ kind: "real" });

        let appConfig: AppConfig = {
            azureAdAppId: appBootstrapModel.azureAdAppId,
            allowMocks: appBootstrapModel.allowMocks,
            environmentType: appBootstrapModel.environmentType,
            lockdown: appBootstrapModel.lockdown,
            mode: this.getMode(appBootstrapModel),
            behaviors: []
        };
        dataModule.constant("appConfig", appConfig);

        dataModule.constant("preLoadedCmsContentItems", appBootstrapModel.preLoadedCmsContentItems);

        if (!appBootstrapModel.i9nMode) {
            let user = authCtx.getActiveAccount();
            initializeMeControl(user);
        }

        angular.element(document).ready(() => {
            angular.bootstrap(document.body, [appModule.name], {
                strictDi: true
            });
        });
    }

    public createAuthContext(azureAdAppId: string): msal.PublicClientApplication {
        return new msal.PublicClientApplication({
        auth:{
            clientId: azureAdAppId,
            redirectUri: this.getAadReplyUrl(),  //  Reply URL must completely match the URL configured in AAD.
        }, cache:{cacheLocation: "localStorage"}   //  See https://github.com/AzureAD/azure-activedirectory-library-for-js/wiki/Known-issues-on-Edge
        }
        );
    }
    
    private getAadReplyUrl(): string {
        let origin = window.location.origin;
        if (origin && origin[origin.length - 1] !== "/") {
            //  All reply URLs configured in AAD have trailing forward slashes.
            origin += "/";
        }

        return origin;
    }

    private getMode(appBootstrapModel: AppBootstrapModel): Mode {
        return appBootstrapModel.i9nMode ? "i9n" : "normal";
    }
}
