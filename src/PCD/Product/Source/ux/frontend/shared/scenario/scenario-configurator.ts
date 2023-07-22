import { IMocksService } from "../mocks.service";
import { ScenarioName } from "./scenario-types";
import { AppConfig } from "../../module/data.module";

type MethodName<T> = keyof T;
type MethodMock<T> = T[keyof T];

export interface IScenarioConfigurator<T> {
    /**
     * Configure mocked implementation of method for a specific scenario.
     * @params scenarioName Name of scenario.
     * @params methodName Name of method.
     * @returns void.
     */
    configureMethodMock(scenarioName: ScenarioName, methodName: MethodName<T>, fn: MethodMock<T>): void;

    /**
     * Configure mocked implementation of method for `default` scenario.
     * @params methodName Name of method.
     * @returns void.
     */
    configureDefaultMethodMock(methodName: MethodName<T>, fn: MethodMock<T>): void;

    /**
     * Get mocked implementation of method for current scenario.
     * This method uses first-match-wins algorithm for finding mocked implmentations.
     * @params methodName Name of method.
     * @returns MethodMock<T> mocked function implementation.
     */
    getMethodMock(methodName: MethodName<T>): Function;
}

export class ScenarioConfigurator<T> implements IScenarioConfigurator<T> {
    private scenarios: string[];
    private scenarioMap: {
        [scenarioName: string]: {
            [L in MethodName<T>]: MethodMock<T>
        }
    } = {};

    constructor(
        private readonly real: T,
        private readonly mocksService: IMocksService,
        private readonly enforceMockingForI9nMode?: boolean,
    ) {
        this.scenarios = this.mocksService.getScenarios();
    }

    public configureMethodMock(scenarioName: ScenarioName, methodName: MethodName<T>, fn: MethodMock<T>): void {
        this.scenarioMap[scenarioName] || (this.scenarioMap[scenarioName] = <any>{});
        this.scenarioMap[scenarioName][methodName] = fn;
    }

    public configureDefaultMethodMock(methodName: MethodName<T>, fn: MethodMock<T>): void {
        this.configureMethodMock("default", methodName, fn);
    }

    public getMethodMock(methodName: MethodName<T>): Function {
        return <Function><any>this.getTypedMethodMock(methodName);
    }

    private getTypedMethodMock(methodName: MethodName<T>): MethodMock<T> {
        let methodMock: MethodMock<T>;

        //  If I9n mode is active and mocking not allowed for I9n,
        //  pass back the real implementation. We do NOT want frontend mocks for 
        //  I9n mode, except when opted in.
        if (this.mocksService.getCurrentMode() === "i9n" &&
            !this.enforceMockingForI9nMode) {
            methodMock = this.realMethodMock(methodName);
            return methodMock;
        }

        //  Look for exact match in the map.
        let matchedScenarioName = _.find(this.scenarios, (scenarioName: ScenarioName) => {
            return (this.scenarioMap[scenarioName] &&
                this.scenarioMap[scenarioName][methodName]);
        });
        matchedScenarioName && (methodMock = this.scenarioMap[matchedScenarioName][methodName]);

        //  If not found, do partial match on each scenario. This will attempt to find base scenarios.
        if (!methodMock) {
            matchedScenarioName = _.find(this.scenarios, (scenarioName: ScenarioName) => {
                let scenarioNameSubStr = <string> scenarioName;

                while (scenarioNameSubStr.lastIndexOf(".") > 0) {
                    //  Partition on delimiter to extract parent scenarios.
                    scenarioNameSubStr = scenarioNameSubStr.substr(0, scenarioNameSubStr.lastIndexOf("."));

                    //  Look for the parent scenario within map. 
                    if (this.scenarioMap[scenarioNameSubStr] &&
                        this.scenarioMap[scenarioNameSubStr][methodName]) {
                        methodMock = this.scenarioMap[scenarioNameSubStr][methodName];
                    }
                }
                return !!methodMock;
            });
        }

        //  If not found, look up the default scenario.
        const defaultScenario = "default";
        if (!methodMock && this.scenarioMap[defaultScenario]) {
            methodMock = this.scenarioMap[defaultScenario][methodName];
        }

        //  If still not found, fall back to real implementation.
        if (!methodMock) {
            console.debug(`Falling back to real implementation for ${methodName}`);

            //  Note: `methodMock` cannot be typed here as `MethodMock<T>` because `_.bind` always returns back
            //  a wrapped method of type `() => any`.
            methodMock = this.realMethodMock(methodName);
        }

        return methodMock;
    }

    private realMethodMock(methodName: MethodName<T>): MethodMock<T> {
        return <any>_.bind(<any>this.real[methodName], this.real);
    }
}
