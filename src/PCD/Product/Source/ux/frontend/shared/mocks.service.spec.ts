import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { AppConfig } from "../module/data.module";
import { ITestableMocksService } from "./mocks.service";

describe("Mocks service", () => {
    describe("when mocks are disallowed", () => {
        let spec: TestSpec;
        let mocksService: ITestableMocksService;

        beforeEach(() => {
            spec = new TestSpec();

            //  Inject AppConfig, so the actual object in use by Angular would be modified.
            inject((_appConfig_: AppConfig) => {
                _appConfig_.allowMocks = false;
            });

            inject((_mocksService_: ITestableMocksService) => {
                mocksService = _mocksService_;
            });
        });

        it("indicates that mocks mode is not active", () => {
            expect(mocksService.isActive()).toBe(false);
        });

        it("ignores query string parameter that enables mocks", () => {
            inject((
                _$location_: ng.ILocationService
            ) => {
                let $locationMocks = new SpyCache(_$location_);
                $locationMocks.getFor("search").and.returnValue({
                    mocks: "true"
                });
            });

            expect(mocksService.isActive()).toBe(false);
        });

        it("throws if list of mock scenarios is accessed", () => {
            expect(() => mocksService.getScenarios()).toThrowError();
        });

        it("throws if list of mock flights is accessed", () => {
            expect(() => mocksService.getFlights()).toThrowError();
        });
    });

    describe("when mocks are allowed", () => {
        let spec: TestSpec;
        let mocksService: ITestableMocksService;
        let $cookiesMocks: SpyCache<ng.cookies.ICookiesService>;
        let $locationMocks: SpyCache<ng.ILocationService>;

        const MocksModeIndicatorCookie = "pcd-mocks";
        const MockScenariosCookie = "pcd-scenarios";
        const MockFlightsCookie = "pcd-flights";

        beforeEach(() => {
            spec = new TestSpec();

            //  Inject AppConfig, so the actual object in use by Angular would be modified.
            inject((_appConfig_: AppConfig) => {
                _appConfig_.allowMocks = true;
            });
        });

        describe("and mock query params are specified", () => {

            it("removes mock-related cookies, if mocks are requested to be disabled", () => {
                inject((
                    _$cookies_: ng.cookies.ICookiesService,
                    _$location_: ng.ILocationService,
                    _mocksService_: ITestableMocksService,
                ) => {
                    $cookiesMocks = new SpyCache(_$cookies_);
                    $cookiesMocks.getFor("remove").and.stub();
                    $cookiesMocks.failIfCalled("get");
                    $cookiesMocks.failIfCalled("put");

                    $locationMocks = new SpyCache(_$location_);
                    $locationMocks.getFor("search").and.returnValue({
                        mocks: "false",
                        scenarios: "scenarioX,scenarioY",
                        flights: "flightX,flightY"
                    });

                    mocksService = _mocksService_;
                });

                mocksService.testableInitialize();

                expect($cookiesMocks.getFor("remove")).toHaveBeenCalledWith(MocksModeIndicatorCookie);
                expect($cookiesMocks.getFor("remove")).toHaveBeenCalledWith(MockScenariosCookie);
                expect($cookiesMocks.getFor("remove")).toHaveBeenCalledWith(MockFlightsCookie);

                //  Simulate absence of cookies.
                $cookiesMocks.getFor("get").and.returnValue(undefined);

                expect(mocksService.isActive()).toBe(false);
                expect(() => mocksService.getScenarios()).toThrowError();
                expect(() => mocksService.getFlights()).toThrowError();
            });

            it("overrides mocking parameters, if mocking is active and mocking query string parameters are present", () => {
                inject((
                    _$cookies_: ng.cookies.ICookiesService,
                    _$location_: ng.ILocationService,
                    _mocksService_: ITestableMocksService,
                ) => {
                    mocksService = _mocksService_;

                    $cookiesMocks = new SpyCache(_$cookies_);
                    $cookiesMocks.failIfCalled("get");
                    $cookiesMocks.getFor("put").and.stub();
                    $cookiesMocks.failIfCalled("remove");

                    $locationMocks = new SpyCache(_$location_);
                    $locationMocks.getFor("search").and.returnValue({
                        mocks: "true",
                        scenarios: "scenarioX,scenarioY",
                        flights: "flightX,flightY"
                    });
                });

                mocksService.testableInitialize();

                expect($cookiesMocks.getFor("put")).toHaveBeenCalledWith(MocksModeIndicatorCookie, "1", { secure: true });
                expect($cookiesMocks.getFor("put")).toHaveBeenCalledWith(MockScenariosCookie, JSON.stringify(["scenarioX", "scenarioY"]), { secure: true });
                expect($cookiesMocks.getFor("put")).toHaveBeenCalledWith(MockFlightsCookie, JSON.stringify(["flightX", "flightY"]), { secure: true });

                //  Simulate cookies.
                $cookiesMocks.getFor("get").and.callFake((cookieName: string) => {
                    switch (cookieName) {
                        case MocksModeIndicatorCookie:
                            return "1";
                        case MockScenariosCookie:
                            return JSON.stringify(["scenarioX", "scenarioY"]);
                        case MockFlightsCookie:
                            return JSON.stringify(["flightX", "flightY"]);
                        default:
                            throw new Error(`Cookie ${cookieName} was not expected at this time.`);
                    }
                });

                expect(mocksService.isActive()).toBe(true);
                expect(mocksService.getScenarios()).toEqual(["scenarioX", "scenarioY"]);
                expect(mocksService.getFlights()).toEqual(["flightX", "flightY"]);
            });

        });

        describe("and mock query params are not specified", () => {
            it("gets mock active flag, list of scenarios and flights when mocks are inactive", () => {
                inject((
                    _$cookies_: ng.cookies.ICookiesService,
                    _$location_: ng.ILocationService,
                    _mocksService_: ITestableMocksService,
                ) => {
                    $cookiesMocks = new SpyCache(_$cookies_);
                    $cookiesMocks.failIfCalled("remove");
                    $cookiesMocks.failIfCalled("put");
                    $cookiesMocks.getFor("get").and.returnValue(undefined);

                    $locationMocks = new SpyCache(_$location_);
                    $locationMocks.getFor("search").and.returnValue({});

                    mocksService = _mocksService_;
                });

                mocksService.testableInitialize();

                expect(mocksService.isActive()).toBe(false);
                expect(() => mocksService.getScenarios()).toThrowError();
                expect(() => mocksService.getFlights()).toThrowError();
            });

            it("gets mock active flag, list of scenarios and flights when mocks are active", () => {
                inject((
                    _$cookies_: ng.cookies.ICookiesService,
                    _$location_: ng.ILocationService,
                    _mocksService_: ITestableMocksService,
                ) => {
                    $cookiesMocks = new SpyCache(_$cookies_);
                    $cookiesMocks.failIfCalled("put");
                    $cookiesMocks.failIfCalled("remove");
                    $cookiesMocks.getFor("get").and.callFake((cookieName: string) => {
                        switch (cookieName) {
                            case MocksModeIndicatorCookie:
                                return "1";
                            case MockScenariosCookie:
                                return JSON.stringify(["scenarioA", "scenarioB"]);
                            case MockFlightsCookie:
                                return JSON.stringify(["flightA", "flightB"]);
                            default:
                                throw new Error(`Cookie ${cookieName} was not expected at this time.`);
                        }
                    });

                    $locationMocks = new SpyCache(_$location_);
                    $locationMocks.getFor("search").and.returnValue({});

                    mocksService = _mocksService_;
                });

                mocksService.testableInitialize();

                expect(mocksService.isActive()).toBe(true);
                expect(mocksService.getScenarios()).toEqual(["scenarioA", "scenarioB"]);
                expect(mocksService.getFlights()).toEqual(["flightA", "flightB"]);
            });

        });
    });

});
