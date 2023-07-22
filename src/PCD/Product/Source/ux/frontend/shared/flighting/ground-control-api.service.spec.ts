import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { IAjaxService } from "../ajax.service";
import { IGroundControlApiService } from "./ground-control-api.service";

describe("Ground Control API service", () => {
    let groundControlApiService: IGroundControlApiService;
    let ajaxServiceMock: SpyCache<IAjaxService>;

    beforeEach(() => {
        let spec = new TestSpec({
            returnMockedAjaxService: true,
            mockedAjaxServiceOptions: {
                authTokenManager: null
            }
        });
        ajaxServiceMock = spec.ajaxServiceMock;

        inject((_groundControlApiService_: IGroundControlApiService) => {
            groundControlApiService = _groundControlApiService_;
        });
    });

    it("gets user flights", () => {
        ajaxServiceMock.getFor("get").and.stub();

        groundControlApiService.getUserFlights();
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/api/getuserflights",
            serviceName: "PdmsUx",
            operationName: "GetUserFlights",
            cache: true,
            maxRetry: 0,
            timeout: 3000
        });
    });
});
