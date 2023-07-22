import { Service, Inject } from "../../module/app.module";

import { IVariantAdminApiService } from "./variant-admin-api.service";
import { VariantRequest } from "../variant/variant-types";

export interface IVariantAdminDataService {
    //  Determines if authenticated user has access to perform variant admin operations.
    hasAccessForVariantAdmin(): ng.IPromise<any>;

    //  Gets all variant requests (by definition, if a request exists, it is pending).
    getAllVariantRequests(): ng.IPromise<VariantRequest[]>;

    //  Approves the specified variant request.
    approveVariantRequest(variantId: string): ng.IPromise<void>;

    //  Denies the specified variant request.
    denyVariantRequest(variantId: string): ng.IPromise<void>;
}

@Service({
    name: "variantAdminDataService"
})
@Inject("variantAdminApiService")
class VariantAdminDataService implements IVariantAdminDataService {
    constructor(
        private variantAdminApiService: IVariantAdminApiService) { }

    public hasAccessForVariantAdmin(): ng.IPromise<any> {
        return this.variantAdminApiService.hasAccessForVariantAdmin();
    }

    public getAllVariantRequests(): ng.IPromise<VariantRequest[]> {
        return this.variantAdminApiService.getAllVariantRequests()
            .then((response: ng.IHttpPromiseCallbackArg<VariantRequest[]>) => {
                return response.data;
            });
    }

    public approveVariantRequest(variantId: string): ng.IPromise<void> {
        return this.variantAdminApiService.approveVariantRequest(variantId)
            .then((response: ng.IHttpPromiseCallbackArg<void>) => {
                return;
            });
    }

    public denyVariantRequest(variantId: string): ng.IPromise<void> {
        return this.variantAdminApiService.denyVariantRequest(variantId)
            .then((response: ng.IHttpPromiseCallbackArg<void>) => {
                return;
            });
    }
}
