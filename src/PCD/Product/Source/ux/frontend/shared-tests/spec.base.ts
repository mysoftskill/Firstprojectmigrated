import "angular";
import "angular-mocks";
import "angular-ui-router";
import "angular-sanitize";

import * as angular from "angular";
import { appModule } from "../module/app.module";
import { dataModule, AppConfig } from "../module/data.module";
import "../module/app.bootstrap";

import { DataServiceMocks } from "./data-service-mocks";

import { IMsalTokenManager, MsalTokenManager, registerMsalTokenManagerFactory } from "../shared/msal-token-manager";
import { IAjaxServiceOptions, IAjaxService, registerAjaxServiceFactory } from "../shared/ajax.service";
import { UiRouterStateMock } from "./ui-router-state-mocks";
import { SpyCache } from "./spy-cache";
import { CmsKey, CmsContentCollection } from "../shared/cms/cms-types";
import { getWrappedCmsContent } from "../shared/cms/cms-utilities";
import { IMocksService } from "../shared/mocks.service";

export * from "./spy-cache";

//  Options for bootstrapping spec.
export interface BootstrapSpecOptions {
    /**
     * If true, inject() won't be executed at all during bootstrap. This results in none of
     * default mocks being applied nor default dependencies resolved.
     */
    doNotRunInject?: boolean;

    /**
     * If true, instance of AJAX service mocks will be returned. This is supposed to be
     * used by low-level tests. Regular tests should mock PDMS data service calls instead.
     */
    returnMockedAjaxService?: boolean;

    /**
     * If provided, instance of AJAX service will be configured with particular options.
     * This is supposed to be used by low-level tests. Regular tests should mock data service
     * calls instead.
     */
    mockedAjaxServiceOptions?: IAjaxServiceOptions;

    //  Pre-loaded content needs to be present for UT's
    preLoadedCmsContents?: CmsContentCollection;
}

//  Component factory options.
export interface CreateComponentOptions {
    //  Markup to instantiate the component.
    markup: string;

    //  Data used by component.
    data?: {};

    //  Controller name override. By default '$ctrl' will be used.
    controllerName?: string;

    /**
     * Indicates that $scope.$digest() should not be invoked right after component initiation.
     * Set this to true, if you want to control when promises used in initializer are resolved.
     */
    doNotRunDigest?: boolean;

    /**
     * Provide Fake contents to be injected to the component.
     */
    fakeCmsContentItems?: FakeCmsContentItem[];
}

export interface FakeCmsContentItem {
    cmsKey: CmsKey;
    content: any;
} 

/** 
 * Result of component factory. 
 **/
export interface ComponentInstance<T> {
    //  Component instance.
    instance: T;

    //  Component scope.
    scope: ng.IScope;

    //  DOM element associated with the component.
    element: JQuery;
}

/**
 * Initializes test infrastructure for PDMS UX.
 */
export class TestSpec {
    //  Application configuration.
    public appConfig: AppConfig;

    //  Angular injector service.
    public $injector: ng.auto.IInjectorService;

    //  Angular $rootScope.
    public $rootScope: ng.IRootScopeService;

    //  Angular compile service.
    public $compile: ng.ICompileService;

    //  Angular Q service.
    public $promises: ng.IQService;

    //  Angular ui-router state service.
    public $state: UiRouterStateMock;

    //  OneUi component registry service.
    public $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService;

    //  Data service mocks.
    public dataServiceMocks: DataServiceMocks;

    /**
     * AJAX service mock. This will be set only if returnMockedAjaxService option was set to true. Should be
     * used only by low-level tests. Regular tests should mock the data service calls instead.
     */
    public ajaxServiceMock?: SpyCache<IAjaxService>;

    /**
     * Constructor.
     * @param specOptions Options for bootstrapped spec. Do not set, unless you really-really need to customize default behavior.
     */
    constructor(private readonly specOptions?: BootstrapSpecOptions) {
        //  Initialize Bradbury with no actual sinks.
        let provider = new Bradbury.TelemetryProvider({
            allowAutoPageView: true,
            flights: []
        });
        provider.useAsGlobalTelemetryProvider();

        this.bootstrapAppForTesting();

        if (!specOptions || !specOptions.doNotRunInject) {
            inject((
                _$rootScope_: ng.IRootScopeService,
                _$q_: ng.IQService,
                _$compile_: ng.ICompileService,
                _$injector_: ng.auto.IInjectorService,
                _$meeComponentRegistry_: MeePortal.OneUI.Angular.IMeeComponentRegistryService
            ) => {
                this.$rootScope = _$rootScope_;
                this.$injector = _$injector_;
                this.$compile = _$compile_;
                this.$promises = _$q_;
                this.$meeComponentRegistry = _$meeComponentRegistry_;
                this.$state = new UiRouterStateMock();                          //  NOTE: this will run inject(), must be under !doNotRunInject condition.

                // This is needed to override the behavior of MonitorOperationProgress within progress views.
                MeePortal.OneUI.Angular.__overrideMonitorOperationProgressInjectorForTests(_$injector_);

                //  Mock data services. Must be initialized after AJAX service was mocked.
                this.dataServiceMocks = new DataServiceMocks();                 //  NOTE: this will run inject(), must be under !doNotRunInject condition.
            });
        }
    }

    /**
     * Component factory.
     * @param options Factory options.
     */
    public createComponent<T>(options: CreateComponentOptions): ComponentInstance<T> {
        let scope = this.$rootScope.$new();
        if (options.data) {
            scope = _.extend({}, options.data, scope);
        }
        
        this.setupCmsMocks(options.fakeCmsContentItems);

        let element = this.$compile(options.markup)(scope);
        let component = <T> element.children().scope()[options.controllerName || "$ctrl"];

        if (!options.doNotRunDigest) {
            scope.$digest();
        }

        //  Expose $injector for MeePortal.OneUI.Angular.MonitorOperationProgress.
        (<any> component).__getAngularInjector = () => this.$injector;

        return {
            instance: component,
            scope,
            element
        };
    }

    /**
     * Invokes digest cycle on root scope.
     * Use this to resolve pending promises.
     */
    public runDigestCycle(): void {
        this.$rootScope.$digest();
    }

    /**
     * Invokes the callback when the promise resolves, otherwise it fails the test.
     * @param promise The promise to wait on until it resolves.
     * @param callback The callback to invoke.
     */
    public awaitResolve(promise: ng.IPromise<any>, callback: (...args: any[]) => void): void {
        promise
            .then(callback)
            .catch(() => {
                fail("Expected to resolve.");
            });

        this.runDigestCycle();
    }

    /**
     * Invokes the callback when the promise rejects, otherwise it fails the test.
     * @param promise The promise to wait on until it rejects.
     * @param callback The callback to invoke.
     */
    public awaitReject(promise: ng.IPromise<any>, callback: (...args: any[]) => void): void {
        promise
            .then(() => {
                fail("Expected to reject.");
            })
            .catch(callback);

        this.runDigestCycle();
    }

    /**
     * Converts data into something that was possibly returned by HTTP operation.
     * @param data Data to convert.
     */
    public asHttpPromise<T>(data: T): ng.IPromise<ng.IHttpPromiseCallbackArg<T>> {
        let result: ng.IHttpPromiseCallbackArg<T> = {
            //  Make a copy of data, because some operations will attempt to update it by copying same object into itself.
            data: angular.copy(data),
            status: null,
            statusText: null,
            headers: null,
            config: null,
            xhrStatus: null
        };
        return this.$promises.resolve(result);
    }

    //  Bootstraps the app with all necessary mocking and stubbing.
    private bootstrapAppForTesting(): void {
        //  Mock app module.
        angular.mock.module(appModule.name);

        //  Initialize data module with default values.
        this.appConfig = {
            azureAdAppId: "AzureAppId",
            allowMocks: false,
            environmentType: "int",
            mode: "normal",
            behaviors: ["disable-automatic-flight-discovery"]
        };
        dataModule.constant("appConfig", this.appConfig);

        dataModule.value("preLoadedCmsContentItems", (this.specOptions && this.specOptions.preLoadedCmsContents) || {});

        this.mockAjaxService();

        let fakeCorrelationContext = this.createFakeCorrelationContextManager();
        appModule.service("correlationContext", () => fakeCorrelationContext);
    }

    //  Mocks AJAX service.
    private mockAjaxService(): void {
        let mockedAjaxServiceOptions: IAjaxServiceOptions = this.specOptions && this.specOptions.mockedAjaxServiceOptions;
        let mockMsalTokenManager = (mockedAjaxServiceOptions && mockedAjaxServiceOptions.authTokenManager) || new MsalTokenManager({
            $q: null,
            authCtx: null,
            resource: null
        });
        let mockMsalTokenManagerSpy = new SpyCache<IMsalTokenManager>(mockMsalTokenManager);

        //  Provide mocked MSAL token factory.
        registerMsalTokenManagerFactory({
            kind: "mock",
            createMockInstance: ($q: ng.IQService, resource: string) => {
                mockMsalTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.resolve("TestMock_InvalidToken"));
                return mockMsalTokenManager;
            }
        });

        let mockAjaxService: IAjaxService = {
            get: () => { throw new Error("Not implemented"); },
            put: () => { throw new Error("Not implemented"); },
            post: () => { throw new Error("Not implemented"); },
            del: () => { throw new Error("Not implemented"); },
            getAntiforgeryToken: () => { throw new Error("Not implemented"); }
        };
        let mockAjaxServiceSpy = new SpyCache(mockAjaxService);

        mockAjaxServiceSpy.failIfCalled("get", "'get' was not expected at this time.");
        mockAjaxServiceSpy.failIfCalled("post", "'post' was not expected at this time.");
        mockAjaxServiceSpy.failIfCalled("put", "'put' was not expected at this time.");
        mockAjaxServiceSpy.failIfCalled("del", "'del' was not expected at this time.");
        mockAjaxServiceSpy.failIfCalled("getAntiforgeryToken", "'getAntiforgeryToken' was not expected at this time.");

        //  Provide mocked AJAX service factory.
        registerAjaxServiceFactory({
            kind: "mock",
            createMockInstance: ($q: ng.IQService, $rootScope: ng.IRootScopeService, appConfig: AppConfig,
                mockService: IMocksService, options: IAjaxServiceOptions) => {

                return mockAjaxService;
            }
        });

        //  Expose AJAX service mock, if requested.
        this.ajaxServiceMock = this.specOptions && this.specOptions.returnMockedAjaxService && mockAjaxServiceSpy;
    }

    //  Creates fake implementation of ICorrelationContextManager interface. UTs are expected to mock the functionality.
    private createFakeCorrelationContextManager(): Bradbury.ICorrelationContextManager {
        return {
            serialize: () => {
                throw new Error("'serialize' was not expected at this time");
            },
            deleteProperty: () => {
                throw new Error("'deleteProperty' was not expected at this time");
            },
            getProperty: () => {
                throw new Error("'getProperty' was not expected at this time");
            },
            setProperty: () => {
                throw new Error("'setProperty' was not expected at this time");
            }
        };
    }

    /** Setup CMS mocks to be injected into a component  
    * This might have to be revisited later. This is V1 implementation, good enough to get us going for now 
    **/
    private setupCmsMocks(cmsContentItems: FakeCmsContentItem[]): void {
        if(!cmsContentItems) {
            return;
        }
        
        this.dataServiceMocks.cmsDataService.getFor("getContentItem").and.callFake((cmsKey: CmsKey) => {
            let cmsContentItem = _.first(_.where(cmsContentItems,  
                (item: FakeCmsContentItem) => item.cmsKey.cmsId === cmsKey.cmsId && item.cmsKey.areaName === cmsKey.areaName));

            if(!cmsContentItem) {
                throw `CMS Content for cmsKey { areaName: ${cmsKey.areaName}, cmsId: ${cmsKey.cmsId} } is not mocked`;
            }

            return getWrappedCmsContent(cmsContentItem.content);
        });
    }
}
