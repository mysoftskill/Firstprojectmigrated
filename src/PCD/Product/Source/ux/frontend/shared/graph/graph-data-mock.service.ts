import * as angular from "angular";
import { Config, Inject } from "../../module/app.module";

import * as GraphTypes from "./graph-types";
import { IGraphDataService } from "./graph-data.service";
import { ScenarioConfigurator, IScenarioConfigurator } from "../scenario/scenario-configurator";

import { IMocksService } from "../mocks.service";
import { AppConfig } from "../../module/data.module";
import { GraphDataMockHelper } from "./graph-data-mock-helper";

/**
 * Mocks Graph data service.
 */
class GraphDataMockService implements IGraphDataService {
    @Config()
    @Inject("$provide")
    public static configureGraphDataMockService($provide: ng.auto.IProvideService): void {
        //  Decorate AJAX service with a function that will add authentication header to each outgoing request.
        $provide.decorator("graphDataService", ["$delegate", "$q", "mocksService", 
            (
                $delegate: IGraphDataService,
                $q: ng.IQService,
                mocksService: IMocksService,
            ): IGraphDataService => {
                return mocksService.isActive() ? new GraphDataMockService(
                    $delegate,
                    $q,
                    mocksService,
                ) : $delegate;
            }
        ]);
    }

    private readonly scenarioConfigurator: IScenarioConfigurator<IGraphDataService>;
    private readonly mockHelper: GraphDataMockHelper;

    constructor(
        private readonly real: IGraphDataService,
        private readonly $promises: ng.IQService,
        private readonly mocksService: IMocksService,
    ) {
        console.debug("Using mocked Graph service.");

        this.scenarioConfigurator = new ScenarioConfigurator(
            this.real,
            this.mocksService,
            true // enforceMockingForI9nMode 
        );
        this.mockHelper = new GraphDataMockHelper(this.$promises);
        this.configureScenarioBasedMocks();
    }

    public getSecurityGroupsWithPrefix(prefixString: string): ng.IPromise<GraphTypes.Group[]> {
        return this.scenarioConfigurator.getMethodMock("getSecurityGroupsWithPrefix")(prefixString);
    }

    public isSecurityGroupNameValid(name: string): boolean {
        return true;
    }

    public getSecurityGroupFromCache(name: string): GraphTypes.Group {
        return this.scenarioConfigurator.getMethodMock("getSecurityGroupFromCache")(name);
    }

    public getSecurityGroupById(id: string, lookupCache?: boolean): ng.IPromise<GraphTypes.Group> {
        return this.scenarioConfigurator.getMethodMock("getSecurityGroupById")(id, lookupCache);
    }

    public getApplicationsWithPrefix(prefixString: string): ng.IPromise<GraphTypes.Application[]> {
        return this.scenarioConfigurator.getMethodMock("getApplicationsWithPrefix")(prefixString);
    }

    public isApplicationNameValid(name: string): boolean {
        return true;
    }

    public getApplicationFromCache(name: string): GraphTypes.Application {
        return this.scenarioConfigurator.getMethodMock("getApplicationFromCache")(name);
    }

    public getApplicationById(id: string, lookupCache?: boolean): ng.IPromise<GraphTypes.Application> {
        return this.scenarioConfigurator.getMethodMock("getApplicationById")(id, lookupCache);
    }

    public getContactsWithPrefix(prefixString: string): ng.IPromise<any> {
        return this.$promises.resolve();
    }

    public getContactById(id: string): ng.IPromise<GraphTypes.Contact> {
        return this.scenarioConfigurator.getMethodMock("getContactById")(id);
    }

    public getContactByEmail(email: string): ng.IPromise<GraphTypes.Contact> {
        return this.scenarioConfigurator.getMethodMock("getContactByEmail")(email);
    }

    public getContactFromCache(name: string): GraphTypes.Contact {
        return this.scenarioConfigurator.getMethodMock("getContactFromCache")(name);
    }

    public isContactNameValid(name: string): boolean {
        return true;
    }

    private configureScenarioBasedMocks(): void {
        //  Security groups related mocks
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getSecurityGroupsWithPrefix",
            this.mockHelper.createSecurityGroupsFnWithPrefix()
        );
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getSecurityGroupFromCache",
            this.mockHelper.getSecurityGroupFnWithName()
        );
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getSecurityGroupById",
            this.mockHelper.getSecurityGroupFnById()
        );

        //  Applications related mocks
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getApplicationsWithPrefix",
            this.mockHelper.createApplicationsFnWithPrefix()
        );
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getApplicationFromCache",
            this.mockHelper.getApplicationFnWithName()
        );
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getApplicationById",
            this.mockHelper.getApplicationFnById()
        );

        //  Contacts related mocks
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getContactById",
            this.mockHelper.getContactFnById()
        );
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getContactByEmail",
            this.mockHelper.getContactFnByEmail()
        );
        this.scenarioConfigurator.configureDefaultMethodMock(
            "getContactFromCache",
            this.mockHelper.getContactFnFromCache()
        );
    }
}
