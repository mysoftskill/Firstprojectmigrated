import { Service, Inject } from "../../module/app.module";
import { AppConfig } from "../../module/data.module";

import { IMsalTokenManagerFactory } from "../msal-token-manager";
import { IAjaxService, IAjaxServiceFactory, IAjaxServiceOptions } from "../ajax.service";
import { VariantRequest } from "./variant-types";

export interface IVariantApiService {
    //  get variants
    getVariants(): ng.IPromise<any>;

    //  create variant request
    createVariantRequest(request: VariantRequest): ng.IPromise<any>;

    /**
     * Gets all variant requests for an owner.
     * @param ownerId to locate all variant requests associated with an owner.
     * @param assetGroupId optional parameter to locate all variant requests by an owner associated with an asset group.
     */
    getVariantRequestsByOwnerId(ownerId: string, assetGroupId?: string): ng.IHttpPromise<any>;

    /**
     * Gets a variant request.
     * @param id variant request id to locate the request.
     */
    getVariantRequestById(id: string): ng.IHttpPromise<any>;

     /**
     * Deletes a variant request.
     * @param id variant request id to locate the request.
     */
    deleteVariantRequestById(id: string): ng.IHttpPromise<any>;

    /**
     * Removes the given variant from an AssetGroup.
     * @param assetGroupId the asset group id to locate the asset group.
     * @param variantId the variant id to remove.
     * @param eTag the asset group etag.
     */
    unlinkVariant(assetGroupId: string, variantId: string, eTag: string): ng.IHttpPromise<any>;
}

@Service({
    name: "variantApiService"
})
@Inject("appConfig", "msalTokenManagerFactory", "ajaxServiceFactory")
class VariantApiService implements IVariantApiService {
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

    public getVariants(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "variant/api/getvariants",
            serviceName: "PdmsUx",
            operationName: "GetVariants"
        });
    }

    public createVariantRequest(request: VariantRequest): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "variant/api/createvariantrequest",
            serviceName: "PdmsUx",
            operationName: "CreateVariantRequest",
            data: request,
        });
    }

    public deleteVariantRequestById(id: string): ng.IHttpPromise<any> {
        return this.ajaxService.del({
            url: "variant/api/deletevariantrequestbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteVariantRequestById",
            data: { id }
        });
    }

    public getVariantRequestsByOwnerId(ownerId: string, assetGroupId?: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "variant/api/getvariantrequestsbyownerid",
            serviceName: "PdmsUx",
            operationName: "GetVariantRequestsByOwnerId",
            data: { ownerId, assetGroupId }
        });
    }

    public getVariantRequestById(id: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "variant/api/getvariantrequestbyid",
            serviceName: "PdmsUx",
            operationName: "GetVariantRequestById",
            data: { id }
        });
    }

    public unlinkVariant(assetGroupId: string, variantId: string, eTag: string): ng.IHttpPromise<any> {
        return this.ajaxService.del({
            url: "variant/api/unlinkvariant",
            serviceName: "PdmsUx",
            operationName: "UnlinkVariant",
            data: { assetGroupId, variantId, eTag  }
        });
    }
}
