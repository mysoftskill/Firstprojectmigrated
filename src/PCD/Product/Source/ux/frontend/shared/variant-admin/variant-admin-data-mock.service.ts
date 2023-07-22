import { Config, Inject } from "../../module/app.module";

import { IVariantAdminDataService } from "./variant-admin-data.service";
import { IMocksService } from "../mocks.service";
import { VariantRequest } from "../variant/variant-types";

class VariantAdminDataMockService implements IVariantAdminDataService {
    @Config()
    @Inject("$provide")
    public static configureVariantAdminDataMockService($provide: ng.auto.IProvideService): void {
        $provide.decorator("variantAdminDataService", ["$delegate", "mocksService",
            ($delegate: IVariantAdminDataService, mocksService: IMocksService): IVariantAdminDataService => {
                return mocksService.isActive() ? new VariantAdminDataMockService($delegate) : $delegate;
            }
        ]);
    }

    constructor(
        private readonly real: IVariantAdminDataService) {
        console.debug("Using mocked VariantAdmin service.");
    }

    public hasAccessForVariantAdmin(): ng.IPromise<any> {
        return this.real.hasAccessForVariantAdmin();
    }

    public getAllVariantRequests(): ng.IPromise<VariantRequest[]> {
        return this.real.getAllVariantRequests();
    }

    public approveVariantRequest(variantId: string): ng.IPromise<void> {
        return this.real.approveVariantRequest(variantId);
    }

    public denyVariantRequest(variantId: string): ng.IPromise<void> {
        return this.real.denyVariantRequest(variantId);
    }
}
