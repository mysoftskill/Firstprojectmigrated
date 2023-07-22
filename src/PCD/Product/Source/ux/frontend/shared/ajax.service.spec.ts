import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { IMsalTokenManager, MsalTokenManager, MsalTokenManagerOptions } from "./msal-token-manager";
import { IAjaxServiceFactory, IAjaxServiceOptions, IAjaxService, registerAjaxServiceFactory } from "./ajax.service";
import { AuthWarningBannerShow, AuthWarningBannerHide } from "../components/shared/auth-warning-banner/auth-warning-banner.component";

describe("Ajax service", () => {
    let spec: TestSpec;
    let ajaxServiceFactory: IAjaxServiceFactory;
    let $q: ng.IQService;
    let mockAuthTokenManager: IMsalTokenManager;

    let defaultServiceOptions: Bradbury.JQueryTelemetryAjaxSettings = {
        serviceName: "TestService",
        operationName: "TestOperation"
    };

    let fakeUnauthorizedXHRResponse: Partial<JQueryXHR> = { status: 401 };

    beforeEach(() => {
        //  Do not run inject to prevent Angular from bootstrapping all infra.
        spec = new TestSpec({ doNotRunInject: true });

        //  Override mocked AJAX service factory with the real one. This test suite will
        //  mock out underlying $.ajax() infra.
        //  This call must be before inject() is called.
        registerAjaxServiceFactory({ kind: "real" });

        inject((
            _ajaxServiceFactory_: IAjaxServiceFactory,
            _$rootScope_: ng.IRootScopeService,
            _$q_: ng.IQService
        ) => {
            ajaxServiceFactory = _ajaxServiceFactory_;
            spec.$rootScope = _$rootScope_;
            $q = _$q_;
        });

        let msalTokenManagerOptions: MsalTokenManagerOptions = {
            $q: null,
            authCtx: null,
            resource: null,
        };
        mockAuthTokenManager = new MsalTokenManager(msalTokenManagerOptions);
    });

    it("gets antiforgery token value", () => {
        let ajaxService = ajaxServiceFactory.createInstance({ authTokenManager: mockAuthTokenManager });
        let expectedAntiforgeryToken = { name: "test-antiforgery", value: "token" };
        spyOn(window.BradburyTelemetry.ajax, "getAntiForgeryToken").and.returnValue(expectedAntiforgeryToken);

        let antiforgeryToken = ajaxService.getAntiforgeryToken();

        expect(antiforgeryToken).toBe(expectedAntiforgeryToken);
    });

    describe("get", () => {
        let ajaxService: IAjaxService;
        let spy: SpyCache<Bradbury.IAjaxTelemetrySink>;

        beforeEach(() => {
            let authTokenManagerSpy = new SpyCache(mockAuthTokenManager);
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.resolve("Test token."));

            ajaxService = ajaxServiceFactory.createInstance({ authTokenManager: mockAuthTokenManager });
            spy = new SpyCache(window.BradburyTelemetry.ajax);
        });

        it("resolves the angular promise if ajax succeeds", (done: DoneFn) => {
            spy.getFor("ajaxGet").and.returnValue($q.resolve());

            spec.awaitResolve(ajaxService.get(defaultServiceOptions), () => {
                done();
            });
        });

        it("rejects the angular promise if ajax fails", (done: DoneFn) => {
            spy.getFor("ajaxGet").and.returnValue($q.reject(fakeUnauthorizedXHRResponse));

            spec.awaitReject(ajaxService.get(defaultServiceOptions), () => {
                done();
            });
        });

        it("supplies default settings for requests", (done: DoneFn) => {
            spy.getFor("ajaxGet").and.returnValue($q.resolve());
            let expectedSettings: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                additionalHeaders: [],
                requestedWithHeaderBehavior: "header"
            };
            expectedSettings.additionalHeaders["Authorization"] = "Bearer Test token.";

            spec.awaitResolve(ajaxService.get(defaultServiceOptions), () => {
                expect(spy.getFor("ajaxGet")).toHaveBeenCalledWith(expectedSettings);
                done();
            });
        });
    });

    describe("delete", () => {
        let ajaxService: IAjaxService;
        let spy: SpyCache<Bradbury.IAjaxTelemetrySink>;

        beforeEach(() => {
            let authTokenManagerSpy = new SpyCache(mockAuthTokenManager);
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.resolve("Test token."));

            ajaxService = ajaxServiceFactory.createInstance({ authTokenManager: mockAuthTokenManager });
            spy = new SpyCache(window.BradburyTelemetry.ajax);
        });

        it("resolves the angular promise if ajax succeeds", (done: DoneFn) => {
            spy.getFor("ajaxDelete").and.returnValue($q.resolve());

            spec.awaitResolve(ajaxService.del(defaultServiceOptions), () => {
                done();
            });
        });

        it("rejects the angular promise if ajax fails", (done: DoneFn) => {
            spy.getFor("ajaxDelete").and.returnValue($q.reject(fakeUnauthorizedXHRResponse));

            spec.awaitReject(ajaxService.del(defaultServiceOptions), () => {
                done();
            });
        });

        it("supplies default settings for requests", (done: DoneFn) => {
            spy.getFor("ajaxDelete").and.returnValue($q.resolve());
            let expectedSettings: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                additionalHeaders: [],
                requestedWithHeaderBehavior: "header"
            };
            expectedSettings.additionalHeaders["Authorization"] = "Bearer Test token.";

            spec.awaitResolve(ajaxService.del(defaultServiceOptions), () => {
                expect(spy.getFor("ajaxDelete")).toHaveBeenCalledWith(expectedSettings);
                done();
            });
        });
    });

    describe("post", () => {
        let ajaxService: IAjaxService;
        let spy: SpyCache<Bradbury.IAjaxTelemetrySink>;

        beforeEach(() => {
            let authTokenManagerSpy = new SpyCache(mockAuthTokenManager);
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.resolve("Test token."));

            ajaxService = ajaxServiceFactory.createInstance({ authTokenManager: mockAuthTokenManager });
            spy = new SpyCache(window.BradburyTelemetry.ajax);
        });

        it("resolves the angular promise if ajax succeeds", (done: DoneFn) => {
            spy.getFor("ajaxPost").and.returnValue($q.resolve());

            spec.awaitResolve(ajaxService.post(defaultServiceOptions), () => {
                done();
            });
        });

        it("rejects the angular promise if ajax fails", (done: DoneFn) => {
            spy.getFor("ajaxPost").and.returnValue($q.reject(fakeUnauthorizedXHRResponse));

            spec.awaitReject(ajaxService.post(defaultServiceOptions), () => {
                done();
            });
        });

        it("supplies default settings for requests", (done: DoneFn) => {
            spy.getFor("ajaxPost").and.returnValue($q.resolve());
            let expectedSettings: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                additionalHeaders: [],
                requestedWithHeaderBehavior: "header"
            };
            expectedSettings.additionalHeaders["Authorization"] = "Bearer Test token.";

            spec.awaitResolve(ajaxService.post(defaultServiceOptions), () => {
                expect(spy.getFor("ajaxPost")).toHaveBeenCalledWith(expectedSettings);
                done();
            });
        });

        it("does not configure the data as JSON if it is a string", (done: DoneFn) => {
            spy.getFor("ajaxPost").and.returnValue($q.resolve());
            let serviceOptions: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                data: "Not as JSON"
            };
            let expectedSettings: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                additionalHeaders: [],
                requestedWithHeaderBehavior: "header",
                data: "Not as JSON"
            };
            expectedSettings.additionalHeaders["Authorization"] = "Bearer Test token.";

            spec.awaitResolve(ajaxService.post(serviceOptions), () => {
                expect(spy.getFor("ajaxPost")).toHaveBeenCalledWith(expectedSettings);
                done();
            });
        });

        it("configures the data as JSON", (done: DoneFn) => {
            spy.getFor("ajaxPost").and.returnValue($q.resolve());
            let serviceOptions: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                data: [{ "key1": "value1" }, [1, 2, 3]]
            };
            let expectedSettings: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                additionalHeaders: [],
                requestedWithHeaderBehavior: "header",
                data: '[{"key1":"value1"},[1,2,3]]',
                contentType: "application/json; charset=utf-8",
            };
            expectedSettings.additionalHeaders["Authorization"] = "Bearer Test token.";

            spec.awaitResolve(ajaxService.post(serviceOptions), () => {
                expect(spy.getFor("ajaxPost")).toHaveBeenCalledWith(expectedSettings);
                done();
            });
        });
    });

    describe("put", () => {
        let ajaxService: IAjaxService;
        let spy: SpyCache<Bradbury.IAjaxTelemetrySink>;

        beforeEach(() => {
            let authTokenManagerSpy = new SpyCache(mockAuthTokenManager);
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.resolve("Test token."));

            ajaxService = ajaxServiceFactory.createInstance({ authTokenManager: mockAuthTokenManager });
            spy = new SpyCache(window.BradburyTelemetry.ajax);
        });

        it("resolves the angular promise if ajax succeeds", (done: DoneFn) => {
            spy.getFor("ajaxPut").and.returnValue($q.resolve());

            spec.awaitResolve(ajaxService.put(defaultServiceOptions), () => {
                done();
            });
        });

        it("rejects the angular promise if ajax fails", (done: DoneFn) => {
            spy.getFor("ajaxPut").and.returnValue($q.reject(fakeUnauthorizedXHRResponse));

            spec.awaitReject(ajaxService.put(defaultServiceOptions), () => {
                done();
            });
        });

        it("supplies default settings for requests", (done: DoneFn) => {
            spy.getFor("ajaxPut").and.returnValue($q.resolve());
            let expectedSettings: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                additionalHeaders: [],
                requestedWithHeaderBehavior: "header"
            };
            expectedSettings.additionalHeaders["Authorization"] = "Bearer Test token.";

            spec.awaitResolve(ajaxService.put(defaultServiceOptions), () => {
                expect(spy.getFor("ajaxPut")).toHaveBeenCalledWith(expectedSettings);
                done();
            });
        });

        it("does not configure the data as JSON if it is a string", (done: DoneFn) => {
            spy.getFor("ajaxPut").and.returnValue($q.resolve());
            let serviceOptions: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                data: "Not as JSON"
            };
            let expectedSettings: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                additionalHeaders: [],
                requestedWithHeaderBehavior: "header",
                data: "Not as JSON"
            };
            expectedSettings.additionalHeaders["Authorization"] = "Bearer Test token.";

            spec.awaitResolve(ajaxService.put(serviceOptions), () => {
                expect(spy.getFor("ajaxPut")).toHaveBeenCalledWith(expectedSettings);
                done();
            });
        });

        it("configures the data as JSON", (done: DoneFn) => {
            spy.getFor("ajaxPut").and.returnValue($q.resolve());
            let serviceOptions: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                data: [{ "key1": "value1" }, [1, 2, 3]]
            };
            let expectedSettings: Bradbury.JQueryTelemetryAjaxSettings = {
                serviceName: "TestService",
                operationName: "TestOperation",
                additionalHeaders: [],
                requestedWithHeaderBehavior: "header",
                data: '[{"key1":"value1"},[1,2,3]]',
                contentType: "application/json; charset=utf-8",
            };
            expectedSettings.additionalHeaders["Authorization"] = "Bearer Test token.";

            spec.awaitResolve(ajaxService.put(serviceOptions), () => {
                expect(spy.getFor("ajaxPut")).toHaveBeenCalledWith(expectedSettings);
                done();
            });
        });
    });

    describe("unauthorized", () => {
        let ajaxService: IAjaxService;
        let ajaxSpy: SpyCache<Bradbury.IAjaxTelemetrySink>;
        let authTokenManagerSpy: SpyCache<IMsalTokenManager>;
        let rootScopeSpy: SpyCache<ng.IRootScopeService>;

        beforeEach(() => {
            ajaxService = ajaxServiceFactory.createInstance({ authTokenManager: mockAuthTokenManager });
            ajaxSpy = new SpyCache(window.BradburyTelemetry.ajax);
            authTokenManagerSpy = new SpyCache(mockAuthTokenManager);
            rootScopeSpy = new SpyCache(spec.$rootScope);
            rootScopeSpy.getFor("$broadcast").and.callThrough();
        });

        it("throws the original error message on bad token", (done: DoneFn) => {
            let expectedErrorMessage = "This error message.";
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.reject(expectedErrorMessage));

            spec.awaitReject(ajaxService.get(defaultServiceOptions), (result: any) => {
                expect(result).toBe(expectedErrorMessage);
                done();
            });
        });

        it("broadcast unauth on bad token", (done: DoneFn) => {
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.reject());

            spec.awaitReject(ajaxService.get(defaultServiceOptions), () => {
                expect(spec.$rootScope.$broadcast).toHaveBeenCalledWith(AuthWarningBannerShow);
                done();
            });
        });

        it("broadcast unauth on 401", (done: DoneFn) => {
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.resolve());
            ajaxSpy.getFor("ajaxGet").and.returnValue($q.reject(fakeUnauthorizedXHRResponse));

            spec.awaitReject(ajaxService.get(defaultServiceOptions), () => {
                expect(spec.$rootScope.$broadcast).toHaveBeenCalledWith(AuthWarningBannerShow);
                done();
            });
        });

        it("broadcast auth on 200 after a 401", (done: DoneFn) => {
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValues($q.reject(), $q.resolve());
            ajaxSpy.getFor("ajaxGet").and.returnValue($q.resolve());

            spec.awaitReject(ajaxService.get(defaultServiceOptions), () => {
                spec.awaitResolve(ajaxService.get(defaultServiceOptions), () => {
                    expect(spec.$rootScope.$broadcast).toHaveBeenCalledWith(AuthWarningBannerHide);
                    done();
                });
            });
        });

        it("doesnt rebroadcast unauth on bad token", (done: DoneFn) => {
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.reject());

            spec.awaitReject(
                $q.all([
                    ajaxService.get(defaultServiceOptions),
                    ajaxService.put(defaultServiceOptions)
                ]), () => {
                    expect(spec.$rootScope.$broadcast).toHaveBeenCalledWith(AuthWarningBannerShow);
                    expect(spec.$rootScope.$broadcast).toHaveBeenCalledTimes(1);
                    done();
                });
        });

        it("doesnt rebroadcast unauth on 401", (done: DoneFn) => {
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValue($q.resolve());
            ajaxSpy.getFor("ajaxGet").and.returnValue($q.reject(fakeUnauthorizedXHRResponse));

            spec.awaitReject(
                $q.all([
                    ajaxService.get(defaultServiceOptions),
                    ajaxService.get(defaultServiceOptions)
                ]), () => {
                    expect(spec.$rootScope.$broadcast).toHaveBeenCalledWith(AuthWarningBannerShow);
                    expect(spec.$rootScope.$broadcast).toHaveBeenCalledTimes(1);
                    done();
                });
        });

        it("doesnt rebroadcast auth on 200", (done: DoneFn) => {
            authTokenManagerSpy.getFor("getTokenAsync").and.returnValues($q.reject(), $q.resolve(), $q.resolve());
            ajaxSpy.getFor("ajaxGet").and.returnValue($q.resolve());

            spec.awaitReject(ajaxService.put(defaultServiceOptions), () => {
                spec.awaitResolve(
                    $q.all([
                        ajaxService.get(defaultServiceOptions),
                        ajaxService.get(defaultServiceOptions)
                    ]), () => {
                        expect(spec.$rootScope.$broadcast).toHaveBeenCalledWith(AuthWarningBannerHide);
                        expect(spec.$rootScope.$broadcast).toHaveBeenCalledTimes(2);
                        done();
                    });
            });
        });
    });
});
