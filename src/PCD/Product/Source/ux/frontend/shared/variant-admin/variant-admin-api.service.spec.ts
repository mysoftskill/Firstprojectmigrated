import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { IAjaxService } from "../ajax.service";
import { IVariantAdminApiService } from "./variant-admin-api.service";

import "./variant-admin-api.service";

describe("Variant Admin API service", () => {
    let variantAdminApiService: IVariantAdminApiService;
    let ajaxServiceMock: SpyCache<IAjaxService>;

    beforeEach(() => {
        let spec = new TestSpec({
            returnMockedAjaxService: true,
            mockedAjaxServiceOptions: {
                authTokenManager: null
            }
        });
        ajaxServiceMock = spec.ajaxServiceMock;

        inject((_variantAdminApiService_: IVariantAdminApiService) => {
            variantAdminApiService = _variantAdminApiService_;
        });
    });

    it("checks authorization", () => {
        ajaxServiceMock.getFor("get").and.stub();

        variantAdminApiService.hasAccessForVariantAdmin();
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "variant-admin/api/hasaccess",
            serviceName: "PdmsUx",
            operationName: "HasAccess"
        });
    });

    it("gets pending variant requests", () => {
        ajaxServiceMock.getFor("get").and.stub();

        variantAdminApiService.getAllVariantRequests();
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "variant-admin/api/getallvariantrequests",
            serviceName: "PdmsUx",
            operationName: "GetAllVariantRequests"
        });
    });

    it("approves pending variant requests", () => {
        ajaxServiceMock.getFor("post").and.stub();

        variantAdminApiService.approveVariantRequest("anyId");
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "variant-admin/api/approvevariantrequest?variantRequestId=anyId",
            serviceName: "PdmsUx",
            operationName: "ApproveVariantRequest",
        });
    });

    it("denies pending variant requests", () => {
        ajaxServiceMock.getFor("del").and.stub();

        variantAdminApiService.denyVariantRequest("anyId");
        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "variant-admin/api/denyvariantrequest",
            serviceName: "PdmsUx",
            operationName: "DenyVariantRequest",
            data: { variantRequestId: "anyId" }
        });
    });
});
