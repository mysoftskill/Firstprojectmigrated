import * as angular from "angular";
import { TestSpec, SpyCache } from "../../shared-tests/spec.base";

import { IVariantAdminApiService } from "./variant-admin-api.service";
import { IVariantAdminDataService } from "./variant-admin-data.service";

describe("Variant Admin data service", () => {
    let spec: TestSpec;
    let variantAdminDataService: IVariantAdminDataService;
    let variantAdminApiServiceMock: SpyCache<IVariantAdminApiService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_variantAdminApiService_: IVariantAdminApiService, _variantAdminDataService_: IVariantAdminDataService) => {
            variantAdminApiServiceMock = new SpyCache(_variantAdminApiService_);
            variantAdminDataService = _variantAdminDataService_;
        });
    });

    it("gets a boolean indicating authorization for variant admin", (done: DoneFn) => {
        // arrange
        variantAdminApiServiceMock.getFor("hasAccessForVariantAdmin").and.returnValue(spec.asHttpPromise<any>({}));

        // act
        variantAdminDataService.hasAccessForVariantAdmin()
            .then(() => {
                // assert
                done();
            });
        spec.runDigestCycle();
    });


    it("approves variant requests", () => {
        variantAdminApiServiceMock.getFor("approveVariantRequest").and.returnValue(spec.asHttpPromise<any>(null));

        variantAdminDataService.approveVariantRequest("anyId");
        spec.runDigestCycle();

        expect(variantAdminApiServiceMock.getFor("approveVariantRequest")).toHaveBeenCalledWith("anyId");
    });

    it("denies variant requests", () => {
        variantAdminApiServiceMock.getFor("denyVariantRequest").and.returnValue(spec.asHttpPromise<any>(null));

        variantAdminDataService.denyVariantRequest("anyId");
        spec.runDigestCycle();

        expect(variantAdminApiServiceMock.getFor("denyVariantRequest")).toHaveBeenCalledWith("anyId");
    });
});
