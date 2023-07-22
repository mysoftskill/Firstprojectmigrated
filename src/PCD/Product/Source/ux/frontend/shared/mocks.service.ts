import { Service, Inject } from "../module/app.module";
import { AppConfig, Mode } from "../module/data.module";
import { ScenarioName } from "./scenario/scenario-types";

//  Provides access to mocking infrastructure.
export interface IMocksService {
    //  Gets value indicating whether the mocks mode is active.
    isActive(): boolean;

    //  Checks if any of the provided scenarios are active.
    isScenarioActive(...listOfScenarios: string[]): boolean;

    //  Gets list of mock scenarios (returns empty array, if none were specified).
    getScenarios(): string[];

    //  Gets mock list of flights (throws, if mocks are not active).
    getFlights(): string[];

    //  Gets the current mode of application.
    getCurrentMode(): Mode;
}

//  Testable variant of IMocksService.
export interface ITestableMocksService extends IMocksService {
    //  Initializes service. Can be used to re-initialize during unit test run.
    testableInitialize(): void;
}

/**
 * Provides all functionality necessary for mocking functionality of the application.
 */
@Service({
    name: "mocksService"
})
@Inject("appConfig", "$cookies", "$location")
class MocksService implements ITestableMocksService {
    private static readonly UseMocksModeQueryParameter = "mocks";
    private static readonly MockScenariosQueryParameter = "scenarios";
    private static readonly MockFlightsQueryParameter = "flights";
    private static readonly UseMocksModeCookie = `pcd-${MocksService.UseMocksModeQueryParameter}`;
    private static readonly MockScenariosCookie = `pcd-${MocksService.MockScenariosQueryParameter}`;
    private static readonly MockFlightsCookie = `pcd-${MocksService.MockFlightsQueryParameter}`;

    constructor(
        private readonly appConfig: AppConfig,
        private readonly $cookies: ng.cookies.ICookiesService,
        private readonly $location: ng.ILocationService) {

        this.testableInitialize();
    }

    public testableInitialize(): void {
        //  Reset the mocks and scenario session cookies.
        let mocksQueryParam = this.$location.search()[MocksService.UseMocksModeQueryParameter];
        if ("true" === mocksQueryParam && this.appConfig.allowMocks) {
            this.setMockingCookies();
            this.setFlightingCookies();
        } else if ("false" === mocksQueryParam || !this.appConfig.allowMocks) {
            this.unsetMockingCookies();
            this.unsetFlightingCookies();
        } else {
            //  No-op if the mocks param is not set to anything.
        }
    }

    public isActive(): boolean {
        //  Kill switch takes the highest priority.
        if (!this.appConfig.allowMocks) {
            return false;
        }

        //  Look in cookies.
        return !!this.$cookies.get(MocksService.UseMocksModeCookie);
    }

    public getScenarios(): string[] {
        if (!this.isActive()) {
            throw new Error("This method can be called only if mocks are active.");
        }

        //  Look in cookies.
        let scenariosCookie = this.$cookies.get(MocksService.MockScenariosCookie);
        if (scenariosCookie) {
            return JSON.parse(scenariosCookie) || [];
        }

        return [];
    }

    public getFlights(): string[] {
        if (!this.isActive()) {
            throw new Error("This method can be called only if mocks are active.");
        }

        //  Look in cookies.
        let flightsCookie = this.$cookies.get(MocksService.MockFlightsCookie);
        if (flightsCookie) {
            return JSON.parse(flightsCookie) || [];
        }

        return [];
    }

    public getCurrentMode(): Mode {
        return this.appConfig.mode;
    }

    public isScenarioActive(...listOfScenarios: ScenarioName[]): boolean {
        return !!_.intersection(listOfScenarios, this.getScenarios()).length;
    }

    private getListFromQueryParam(name: string, defaultList: string[]): ScenarioName[] {
        let listCandidate = this.$location.search()[name];
        return listCandidate ? listCandidate.split(",") : defaultList;
    }

    private setCookie(name: string, value: string): void {
        this.$cookies.put(name, value, { secure: this.getCurrentMode() !== "i9n" });
    }

    private setMockingCookies(): void {
        let mockScenarios = JSON.stringify(this.getListFromQueryParam(MocksService.MockScenariosQueryParameter, ["default"]));

        this.setCookie(MocksService.UseMocksModeCookie, "1");
        this.setCookie(MocksService.MockScenariosCookie, mockScenarios);

        console.debug(`Mock scenario in use: ${mockScenarios}.`);
    }

    private unsetMockingCookies(): void {
        this.$cookies.remove(MocksService.UseMocksModeCookie);
        this.$cookies.remove(MocksService.MockScenariosCookie);
    }

    private setFlightingCookies(): void {
        let flightListCandidate = this.getListFromQueryParam(MocksService.MockFlightsQueryParameter, []);

        //  Set flights cookie only if query string parameter was present.
        if (flightListCandidate.length) {
            let mockFlights = JSON.stringify(flightListCandidate);
            this.setCookie(MocksService.MockFlightsCookie, mockFlights);

            console.debug(`Mock flights in use: ${mockFlights}.`);
        }
    }

    private unsetFlightingCookies(): void {
        this.$cookies.remove(MocksService.MockFlightsCookie);
    }
}
