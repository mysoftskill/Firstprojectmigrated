import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { IAjaxService } from "../ajax.service";
import { IRegistrationStatusApiService } from "./registration-status-api.service";

describe("Registration status API service", () => {
    let registrationStatusApiService: IRegistrationStatusApiService;
    let ajaxServiceMock: SpyCache<IAjaxService>;

    beforeEach(() => {
        let spec = new TestSpec({
            returnMockedAjaxService: true,
            mockedAjaxServiceOptions: {
                authTokenManager: null
            }
        });
        ajaxServiceMock = spec.ajaxServiceMock;

        inject((_registrationStatusApiService_: IRegistrationStatusApiService) => {
            registrationStatusApiService = _registrationStatusApiService_;
        });
    });

    it("can get agent status", () => {
        // arrange
        ajaxServiceMock.getFor("get").and.stub();

        // act
        registrationStatusApiService.getAgentStatus("agentId1");

        // assert
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "agent-status/api/getagentstatus",
            serviceName: "PdmsUx",
            operationName: "GetAgentStatus",
            data: ({ agentId: "agentId1"})
        });
    });

    it("can get assetGroup status", () => {
        // arrange
        ajaxServiceMock.getFor("get").and.stub();

        // act
        registrationStatusApiService.getAssetGroupStatus("ag1");

        // assert
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "agent-status/api/getassetgroupstatus",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupStatus",
            data: ({ assetGroupId: "ag1"})
        });
    });
});
