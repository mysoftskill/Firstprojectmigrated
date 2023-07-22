import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { IAjaxService } from "../ajax.service";
import { ICmsApiService, CmsKey } from "./cms-types";

describe("Cms API service", () => {
    let cmsApiService: ICmsApiService;
    let ajaxServiceMock: SpyCache<IAjaxService>;

    beforeEach(() => {
        let spec = new TestSpec({
            returnMockedAjaxService: true,
            mockedAjaxServiceOptions: {
                authTokenManager: null
            }
        });
        ajaxServiceMock = spec.ajaxServiceMock;

        inject((_cmsApiService_: ICmsApiService) => {
            cmsApiService = _cmsApiService_;
        });
    });

    it("can get content items", () => {
        ajaxServiceMock.getFor("post").and.stub();

        let cmsKeys: CmsKey[] = [{cmsId: "test-content", areaName: "shared"}];

        cmsApiService.getContentItems(cmsKeys);
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/cms/api/getcontentitems",
            serviceName: "PdmsUx",
            operationName: "GetContentItems",
            dataType: "json",
            data: cmsKeys
        });
    });
});
