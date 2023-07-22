import * as angular from "angular";
import { TestSpec, SpyCache } from "../../shared-tests/spec.base";

import { IRegistrationStatusApiService } from "./registration-status-api.service";
import { IRegistrationStatusDataService } from "./registration-status-data.service";
import { AgentRegistrationStatus } from "./registration-status-types";

describe("Registration status data service", () => {
    let spec: TestSpec;
    let registrationStatusDataService: IRegistrationStatusDataService;
    let registrationStatusApiServiceMock: SpyCache<IRegistrationStatusApiService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_registrationStatusApiService_: IRegistrationStatusApiService, _registrationStatusDataService_: IRegistrationStatusDataService) => {
            registrationStatusApiServiceMock = new SpyCache(_registrationStatusApiService_);
            registrationStatusDataService = _registrationStatusDataService_;
        });
    });

    it("gets agent status", () => {
        // arrange
        let result: AgentRegistrationStatus = null;
        registrationStatusApiServiceMock.getFor("getAgentStatus").and.returnValue(spec.asHttpPromise(result));

        // act
        registrationStatusDataService.getAgentStatus("agentId1");
        spec.runDigestCycle();

        // assert
        expect(registrationStatusApiServiceMock.getFor("getAgentStatus")).toHaveBeenCalledWith("agentId1");
    });

    it("gets assetGroup status", () => {
        // arrange
        let result: AgentRegistrationStatus = null;
        registrationStatusApiServiceMock.getFor("getAssetGroupStatus").and.returnValue(spec.asHttpPromise(result));

        // act
        registrationStatusDataService.getAssetGroupStatus("ag1");
        spec.runDigestCycle();

        // assert
        expect(registrationStatusApiServiceMock.getFor("getAssetGroupStatus")).toHaveBeenCalledWith("ag1");
    });
});
