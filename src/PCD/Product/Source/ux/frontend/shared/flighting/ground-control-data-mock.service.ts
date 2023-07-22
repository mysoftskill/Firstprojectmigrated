import { Config, Inject } from "../../module/app.module";

import { IMocksService } from "../mocks.service";
import { ITestableGroundControlDataService, IGroundControlDataService } from "./ground-control-data.service";
import { StringUtilities } from "../string-utilities";
import { IScenarioConfigurator, ScenarioConfigurator } from "../scenario/scenario-configurator";

class GroundControlDataMockService implements ITestableGroundControlDataService {
    @Config()
    @Inject("$provide")
    public static configureVariantAdminDataMockService($provide: ng.auto.IProvideService): void {
        $provide.decorator("groundControlDataService", ["$delegate", "mocksService", "$q",
            ($delegate: ITestableGroundControlDataService, mocksService: IMocksService, $q: ng.IQService): ITestableGroundControlDataService => {
                return mocksService.isActive() ? new GroundControlDataMockService($delegate, mocksService, $q) : $delegate;
            }
        ]);
    }

    private readonly scenarioConfigurator: IScenarioConfigurator<IGroundControlDataService>;

    constructor(
        private readonly real: ITestableGroundControlDataService,
        private readonly mocksService: IMocksService,
        private readonly $q: ng.IQService) {

        console.debug("Using mocked GroundControlData service.");

        this.scenarioConfigurator = new ScenarioConfigurator(
            this.real,
            this.mocksService
        );

        this.configureMocks();
    }

    public resetState(): void {
        //  Do nothing.
    }

    public initializeForCurrentUser(): ng.IPromise<void> {
        return this.$q.resolve();
    }

    public isUserInFlight(flightName: string): ng.IPromise<boolean> {
        return this.scenarioConfigurator.getMethodMock("isUserInFlight")(flightName);
    }

    private configureMocks(): void {
        this.scenarioConfigurator.configureMethodMock("flighting", "isUserInFlight", flightName => {
            let flights = this.mocksService.getFlights();
            return this.$q.resolve(_.any(flights, userFlight => StringUtilities.areEqualIgnoreCase(userFlight, flightName)));
        });

        this.scenarioConfigurator.configureMethodMock("flighting.no-flights", "isUserInFlight", _ => {
            return this.$q.resolve(false);
        });
    }
}
