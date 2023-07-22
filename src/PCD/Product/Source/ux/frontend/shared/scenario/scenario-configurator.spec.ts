import { TestSpec, SpyCache } from "../../shared-tests/spec.base";

import * as Pdms from "../pdms/pdms-types";
import { ScenarioConfigurator } from "./scenario-configurator";
import { IMocksService } from "../mocks.service";
import { IPdmsDataService } from "../pdms/pdms-types";
import { AppConfig } from "../../module/data.module";

describe("ScenarioConfigurator", () => {
    let spec: TestSpec;
    let mocksServiceMock: SpyCache<IMocksService>;
    let mocksService: IMocksService;
    let appConfig: AppConfig;
    let scenarioConfigurator: ScenarioConfigurator<IPdmsDataService>;

    let dataOwner1: Pdms.DataOwner = {
        id: "team1-id",
        name: "team1-name",
        description: "team1-desc",
        alertContacts: null,
        announcementContacts: null,
        writeSecurityGroups: null,
        sharingRequestContacts: null,
        assetGroups: null,
        dataAgents: null,
        serviceTree: null,
    };

    beforeEach(() => {
        spec = new TestSpec();

        inject((_mocksService_: IMocksService, _appConfig_: AppConfig) => {
            mocksService = _mocksService_;
            mocksServiceMock = new SpyCache(_mocksService_);
            appConfig = _appConfig_;
        });
    });

    it("configures method mocks appropriately (happy case)", () => {
        // arrange
        mocksServiceMock.getFor("getScenarios").and.returnValue(["default"]);
        scenarioConfigurator = new ScenarioConfigurator<IPdmsDataService>(
            spec.dataServiceMocks.pdmsDataService.instance,
            mocksService,
        );
        scenarioConfigurator.configureMethodMock("default", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner1];
        });

        // act/assert
        expect((<Function>scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser"))()[0]).toEqual(dataOwner1);
    });

    it("looks up mock implementations appropriately", () => {
        // arrange
        let dataOwner2: Pdms.DataOwner = {
            id: "team2-id",
            name: "team2-name",
            description: "team2-desc",
            alertContacts: null,
            announcementContacts: null,
            writeSecurityGroups: null,
            sharingRequestContacts: null,
            assetGroups: null,
            dataAgents: null,
            serviceTree: null,
        };
        mocksServiceMock.getFor("getScenarios").and.returnValue(["default-scenario.caseNotExistent", "default-scenario.case1"]);
        scenarioConfigurator = new ScenarioConfigurator<IPdmsDataService>(
            spec.dataServiceMocks.pdmsDataService.instance,
            mocksService,
        );
        scenarioConfigurator.configureMethodMock("default", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner1];
        });
        scenarioConfigurator.configureMethodMock(<any> "default-scenario.case1", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner2];
        });

        // act/assert
        expect((<Function>scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser"))()[0]).toEqual(dataOwner2);
    });

    it("looks up parent mock implementations appropriately", () => {
        // arrange
        let dataOwner2: Pdms.DataOwner = {
            id: "team2-id",
            name: "team2-name",
            description: "team2-desc",
            alertContacts: null,
            announcementContacts: null,
            writeSecurityGroups: null,
            sharingRequestContacts: null,
            assetGroups: null,
            dataAgents: null,
            serviceTree: null,
        };
        mocksServiceMock.getFor("getScenarios").and.returnValue(["default-scenario.caseNotExistent"]);
        scenarioConfigurator = new ScenarioConfigurator<IPdmsDataService>(
            spec.dataServiceMocks.pdmsDataService.instance,
            mocksService,
        );
        scenarioConfigurator.configureMethodMock("default", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner1];
        });
        scenarioConfigurator.configureMethodMock(<any> "default-scenario.case1", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner2];
        });

        // act/assert
        expect((<Function>scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser"))()[0]).toEqual(dataOwner1);
    });

    it("looks up base mock implementations appropriately", () => {
        // arrange
        let dataOwner2: Pdms.DataOwner = {
            id: "team2-id",
            name: "team2-name",
            description: "team2-desc",
            alertContacts: null,
            announcementContacts: null,
            writeSecurityGroups: null,
            sharingRequestContacts: null,
            assetGroups: null,
            dataAgents: null,
            serviceTree: null,
        };
        mocksServiceMock.getFor("getScenarios").and.returnValue(["default-scenario.caseNotExistent.caseNotExistent"]);
        scenarioConfigurator = new ScenarioConfigurator<IPdmsDataService>(
            spec.dataServiceMocks.pdmsDataService.instance,
            mocksService,
        );
        scenarioConfigurator.configureMethodMock("default", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner1];
        });
        scenarioConfigurator.configureMethodMock(<any> "default-scenario.case1", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner2];
        });

        // act/assert
        expect((<Function>scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser"))()[0]).toEqual(dataOwner1);
    });

    it("looks up default scenario if query string scenario is not configured", () => {
        // arrange
        let dataOwner2: Pdms.DataOwner = {
            id: "team2-id",
            name: "team2-name",
            description: "team2-desc",
            alertContacts: null,
            announcementContacts: null,
            writeSecurityGroups: null,
            sharingRequestContacts: null,
            assetGroups: null,
            dataAgents: null,
            serviceTree: null,
        };
        mocksServiceMock.getFor("getScenarios").and.returnValue(["scenarioNotExistent.caseNotExistent"]);
        scenarioConfigurator = new ScenarioConfigurator<IPdmsDataService>(
            spec.dataServiceMocks.pdmsDataService.instance,
            mocksService,
        );
        spec.dataServiceMocks.pdmsDataService.getFor("getOwnersByAuthenticatedUser").and.stub();
        scenarioConfigurator.configureMethodMock("default", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner1];
        });
        scenarioConfigurator.configureMethodMock(<any> "default-scenario.case1", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner2];
        });

        // act/assert
        expect((<Function>scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser"))()[0]).toEqual(dataOwner1);
    });

    it("falls back to real implementation when scenario is not configured", () => {
        // arrange
        let dataOwner2: Pdms.DataOwner = {
            id: "team2-id",
            name: "team2-name",
            description: "team2-desc",
            alertContacts: null,
            announcementContacts: null,
            writeSecurityGroups: null,
            sharingRequestContacts: null,
            assetGroups: null,
            dataAgents: null,
            serviceTree: null,
        };
        mocksServiceMock.getFor("getScenarios").and.returnValue(["scenarioNotExistent.caseNotExistent"]);
        scenarioConfigurator = new ScenarioConfigurator<IPdmsDataService>(
            spec.dataServiceMocks.pdmsDataService.instance,
            mocksService,
        );
        spec.dataServiceMocks.pdmsDataService.getFor("getOwnersByAuthenticatedUser").and.stub();
        scenarioConfigurator.configureMethodMock(<any> "default-scenario.case1", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner2];
        });

        // act
        (<Function>scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser"))();

        // assert
        expect(spec.dataServiceMocks.pdmsDataService.getFor("getOwnersByAuthenticatedUser")).toHaveBeenCalled();
    });

    it("falls back to real implementation during i9n mode", () => {
        // arrange
        mocksServiceMock.getFor("getScenarios").and.returnValue(["default"]);
        mocksServiceMock.getFor("getCurrentMode").and.returnValue("i9n");
        scenarioConfigurator = new ScenarioConfigurator<IPdmsDataService>(
            spec.dataServiceMocks.pdmsDataService.instance,
            mocksService,
        );
        spec.dataServiceMocks.pdmsDataService.getFor("getOwnersByAuthenticatedUser").and.stub();
        scenarioConfigurator.configureMethodMock("default", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner1];
        });

        // act
        (<Function>scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser"))();

        // assert
        expect(spec.dataServiceMocks.pdmsDataService.getFor("getOwnersByAuthenticatedUser")).toHaveBeenCalled();
    });

    it("uses mocked implementation when i9n mode enforces frontend mocking", () => {
        // arrange
        mocksServiceMock.getFor("getScenarios").and.returnValue(["default"]);
        mocksServiceMock.getFor("getCurrentMode").and.returnValue("i9n");
        scenarioConfigurator = new ScenarioConfigurator<IPdmsDataService>(
            spec.dataServiceMocks.pdmsDataService.instance,
            mocksService,
            true // enforceMockingForI9nMode 
        );
        spec.dataServiceMocks.pdmsDataService.getFor("getOwnersByAuthenticatedUser").and.stub();
        scenarioConfigurator.configureMethodMock("default", "getOwnersByAuthenticatedUser", () => {
            return [dataOwner1];
        });

        // act/assert
        expect((<Function>scenarioConfigurator.getMethodMock("getOwnersByAuthenticatedUser"))()[0]).toEqual(dataOwner1);
    });
});
