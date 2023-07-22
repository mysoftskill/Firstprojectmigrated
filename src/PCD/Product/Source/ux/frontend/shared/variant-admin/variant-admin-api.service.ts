import { Service, Inject } from "../../module/app.module";
import { AppConfig } from "../../module/data.module";

import { IMsalTokenManagerFactory } from "../msal-token-manager";
import { IAjaxService, IAjaxServiceFactory, IAjaxServiceOptions } from "../ajax.service";
import { StringUtilities } from "../string-utilities";

export interface IVariantAdminApiService {
    //  Checks if the user has access to issue variant admin operations.
    hasAccessForVariantAdmin(): ng.IHttpPromise<any>;

    //  Gets all variant requests (by definition, if a request exists, it is pending).
    getAllVariantRequests(): ng.IHttpPromise<any>;

    //  Approves the specified variant request.
    approveVariantRequest(variantId: string): ng.IHttpPromise<any>;

    //  Denies the specified variant request.
    denyVariantRequest(variantId: string): ng.IHttpPromise<any>;
}

@Service({
    name: "variantAdminApiService"
})
@Inject("appConfig", "msalTokenManagerFactory", "ajaxServiceFactory")
class VariantAdminApiService implements IVariantAdminApiService {
    private ajaxService: IAjaxService;

    constructor(
        private readonly appConfig: AppConfig,
        private readonly msalTokenManagerFactory: IMsalTokenManagerFactory,
        private readonly ajaxServiceFactory: IAjaxServiceFactory
    ) {
        let ajaxOptions: IAjaxServiceOptions = {
            authTokenManager: msalTokenManagerFactory.createInstance(this.appConfig.azureAdAppId)
        };
        this.ajaxService = ajaxServiceFactory.createInstance(ajaxOptions);
    }

    public hasAccessForVariantAdmin(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "variant-admin/api/hasaccess",
            serviceName: "PdmsUx",
            operationName: "HasAccess"
        });
    }

    public getAllVariantRequests(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "variant-admin/api/getallvariantrequests",
            serviceName: "PdmsUx",
            operationName: "GetAllVariantRequests"
        });
    }

    public approveVariantRequest(variantRequestId: string): ng.IHttpPromise<any> {
        let queryString = StringUtilities.queryStringOf({variantRequestId});
        return this.ajaxService.post({
            url: `variant-admin/api/approvevariantrequest?${queryString}`,
            serviceName: "PdmsUx",
            operationName: "ApproveVariantRequest"
        });
    }

    public denyVariantRequest(variantRequestId: string): ng.IHttpPromise<any> {
        return this.ajaxService.del({
            url: "variant-admin/api/denyvariantrequest",
            serviceName: "PdmsUx",
            operationName: "DenyVariantRequest",
            data: { variantRequestId }
        });
    }
}
