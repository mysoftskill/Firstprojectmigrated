import { appModule } from "../module/app.module";
import { IMsalTokenManager } from "./msal-token-manager";
import { AuthWarningBannerShow, AuthWarningBannerHide } from "../components/shared/auth-warning-banner/auth-warning-banner.component";
import { AppConfig } from "../module/data.module";
import { IMocksService } from "./mocks.service";

/**
 * Defines a successful Angular response that will be returned from the IPortalAjaxServiceAngular.
 */
export interface IAngularSuccessResponse {
    //  The data returned from the API.
    data: any;

    //  The status text of the response.
    textStatus: string;

    //  The jQuery XHR object.
    jqXHR: JQueryXHR;
}

/**
 * JQueryXHR with better defined error properties (matches JsonErrorModel structure of web role).
 */
export interface JQueryXHRWithErrorResponse extends Partial<JQueryXHR> {
    responseJSON?: {
        //  Error code.
        error: string;
        //  Optional data property bag.
        data?: {
            [key: string]: string
        };
    };
}

/**
 * Defines a failed Angular response that will be returned from the IPortalAjaxServiceAngular.
 */
export interface IAngularFailureResponse {
    //  The jQuery XHR object.
    jqXHR: JQueryXHRWithErrorResponse;

    //  The status text of the response.
    textStatus: string;

    //  The error returned from the API.
    errorThrown: string;
}

/**
 * Interface for configurable optipns of Ajax service.
 */
export interface IAjaxServiceOptions {
    //  Authentication token manager used to get tokens for every request.
    authTokenManager: IMsalTokenManager;
}

/**
 * Interface for AjaxServiceFactory.
 */
export interface IAjaxServiceFactory {
    //  Method to return a AjaxService object.
    createInstance: (options: IAjaxServiceOptions) => IAjaxService;
}

/**
* Defines service that provides AJAX functionality for Angular code on portal.
*/
export interface IAjaxService {
    /**
     * HTTP GET operation.
     * @params options window.BradburyTelemetry.ajax.ajaxGet() options.
     * @returns Angular promise of AJAX operation.
     */
    get(options: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<any>;

    /**
     * HTTP POST operation.
     * @params options window.BradburyTelemetry.ajax.ajaxPost() options.
     * @returns Angular promise of AJAX operation.
     */
    post(options: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<any>;

    /**
     * HTTP PUT operation.
     * @params options window.BradburyTelemetry.ajax.ajaxPut() options.
     * @returns Angular promise of AJAX operation.
     */
    put(options: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<any>;

    /**
     * HTTP DELETE operation.
     * @params options window.BradburyTelemetry.ajax.ajaxDelete() options.
     * @returns Angular promise of AJAX operation.
     */
    del(options: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<any>;

    /**
     * Gets antiforgery token used by the current window.
     * @returns Antiforgery token information.
     */
    getAntiforgeryToken(): Bradbury.AntiforgeryToken;
}

/**
 * Provides in-scope wrapper for window.BradburyTelemetry.ajax.ajax*() operations. All JQuery promises will
 * be resolved during the Angular scope digest cycle.
 */
class AjaxService implements IAjaxService {
    private hasBroadcastShowBanner = false;

    constructor(
        private readonly $q: ng.IQService,
        private readonly $rootScope: ng.IRootScopeService,
        private readonly appConfig: AppConfig,
        private readonly mockService: IMocksService,
        private readonly options: IAjaxServiceOptions,
    ) { }

    //  Part of IAjaxServiceAngular.
    public get = (settings: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<IAngularSuccessResponse> => {
        return this.createAngularPromise(window.BradburyTelemetry.ajax.ajaxGet, settings);
    }

    //  Part of IAjaxServiceAngular.
    public post = (settings: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<IAngularSuccessResponse> => {
        settings = this.configureDataAsJson(settings);
        return this.createAngularPromise(window.BradburyTelemetry.ajax.ajaxPost, settings);
    }

    //  Part of IAjaxServiceAngular.
    public put = (settings: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<IAngularSuccessResponse> => {
        settings = this.configureDataAsJson(settings);
        return this.createAngularPromise(window.BradburyTelemetry.ajax.ajaxPut, settings);
    }

    //  Part of IAjaxServiceAngular.
    public del = (settings: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<IAngularSuccessResponse> => {
        return this.createAngularPromise(window.BradburyTelemetry.ajax.ajaxDelete, settings);
    }

    //  Part of IAjaxServiceAngular.
    public getAntiforgeryToken = (): Bradbury.AntiforgeryToken => {
        return window.BradburyTelemetry.ajax.getAntiForgeryToken();
    }

    /**
     * Converts HTTP operation promise with the settings provided to a generic Angular promise.
     */
    private createAngularPromise(
        ajaxMethod: (settings: Bradbury.JQueryTelemetryAjaxSettings) =>
            JQueryXHR, settings: Bradbury.JQueryTelemetryAjaxSettings): ng.IHttpPromise<any> {

        return this.options.authTokenManager.getTokenAsync()
            .catch((errorMessage: string) => {
                this.showAuthWarningBanner();
                throw errorMessage;
            })
            .then((token: string) => {
                //  Re-configure the settings based on supplied overrides.
                settings.additionalHeaders = settings.additionalHeaders || [];
                settings.additionalHeaders["Authorization"] = `Bearer ${token}`;
                settings.requestedWithHeaderBehavior = "header";

                //  If frontend mocks are enabled, send scenarios in ajax header.
                //  Note that this will pass headers for "FrontendDev" mode too, which has no effect.
                //  Once support for "Emulation" mode is added, evaluate creating a mocked verison of this service.
                if (this.mockService.isActive()) {
                    settings.additionalHeaders["X-Scenarios"] = this.mockService.getScenarios();
                    settings.additionalHeaders["X-Flights"] = this.mockService.getFlights();
                }

                //  Converts HTTP operation promise to a generic Angular promise.
                let jqueryPromise = ajaxMethod(settings);
                let angularDeferred = this.$q.defer<any>();

                //  Unscoped promise resolution will result in resolving scope-aware promise.
                jqueryPromise.then((data, textStatus, jqXHR) => {
                    this.hideAuthWarningBanner();
                    angularDeferred.resolve({ data, textStatus, jqXHR });
                }, (jqXHR, textStatus, errorThrown) => {
                    if (jqXHR.status === 401) {
                        this.showAuthWarningBanner();
                    }
                    angularDeferred.reject({ jqXHR, textStatus, errorThrown });
                });

                return angularDeferred.promise;
            });
    }

    /**
     * Attempts to hide the banner, if the same broadcast has not already been made.
     */
    private hideAuthWarningBanner(): void {
        if (this.hasBroadcastShowBanner) {
            this.hasBroadcastShowBanner = false;
            this.$rootScope.$broadcast(AuthWarningBannerHide);
        }
    }

    /**
     * Attempts to show the banner, if the same broadcast has not already been made.
     */
    private showAuthWarningBanner(): void {
        if (!this.hasBroadcastShowBanner) {
            this.hasBroadcastShowBanner = true;
            this.$rootScope.$broadcast(AuthWarningBannerShow);
        }
    }

    /**
     * Re-configures the settings to send data as JSON.
     */
    private configureDataAsJson(settings: Bradbury.JQueryTelemetryAjaxSettings): Bradbury.JQueryTelemetryAjaxSettings {
        if (settings && settings.data && !_.isString(settings.data)) {
            settings.contentType = "application/json; charset=utf-8";
            settings.data = JSON.stringify(settings.data);
        }

        return settings;
    }
}

export type RegisterAjaxServiceFactoryRealOptions = {
    kind: "real";
};

export type RegisterAjaxServiceFactoryMockOptions = {
    kind: "mock";
    createMockInstance: ($q: ng.IQService, $rootScope: ng.IRootScopeService, appConfig: AppConfig,
        mockService: IMocksService, options: IAjaxServiceOptions) => IAjaxService;
};

export function registerAjaxServiceFactory(factoryOptions: RegisterAjaxServiceFactoryRealOptions | RegisterAjaxServiceFactoryMockOptions): void {
    appModule.factory("ajaxServiceFactory", ["$q", "$rootScope", "appConfig", "mocksService",
        ($q: ng.IQService, $rootScope: ng.IRootScopeService, appConfig: AppConfig, mockService: IMocksService) => {

        return {
            createInstance: (options: IAjaxServiceOptions): IAjaxService => {
                if (factoryOptions.kind === "real") {
                    return new AjaxService($q, $rootScope, appConfig, mockService, options);
                } else {
                    return factoryOptions.createMockInstance($q, $rootScope, appConfig, mockService, options);
                }
            }
        };
    }]);
}
