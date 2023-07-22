import { Service, Inject } from "../../module/app.module";
import { VariantDefinition, AssetGroup } from "../pdms/pdms-types";
import { VariantRequest } from "./variant-types";
import { IVariantApiService } from "./variant-api.service";

export interface IVariantDataService {
    //  get variants
    getVariants(): ng.IPromise<VariantDefinition[]>;

    //  create variant request
    createVariantRequest(request: VariantRequest): ng.IPromise<void>;

    /**
     * Gets all variant requests for an owner.
     * @param ownerId to locate all variant requests associated with an owner.
     * @param assetGroupId optional parameter to locate all variant requests by an owner associated with an asset group.
     */
    getVariantRequestsByOwnerId(ownerId: string, assetGroupId?: string): ng.IPromise<VariantRequest[]>;

    /**
     * Gets a variant request.
     * @param id variant request id to locate the request.
     */
    getVariantRequestById(id: string): ng.IPromise<VariantRequest>;

    /**
     * Deletes a variant request.
     * @param id variant request id to locate the request.
     */
    deleteVariantRequestById(id: string): ng.IPromise<void>;

    /**
     * Removes the given variant from an AssetGroup.
     * @param assetGroupId the asset group id to locate the asset group.
     * @param variantId the variant id to remove.
     * @param eTag the asset group etag.
     */
    unlinkVariant(assetGroupId: string, variantId: string, eTag: string): ng.IPromise<AssetGroup>;
}

@Service({
    name: "variantDataService"
})
@Inject("variantApiService")
class VariantDataService implements IVariantDataService {
    constructor(
        private variantApiService: IVariantApiService) { }

    //  get variants
    public getVariants(): ng.IPromise<VariantDefinition[]> {
        return this.variantApiService.getVariants()
            .then((result: ng.IHttpPromiseCallbackArg<VariantDefinition[]>) => {
                return result.data;
            });
    }

    //  create variant request
    public createVariantRequest(request: VariantRequest): ng.IPromise<void> {
        return this.variantApiService.createVariantRequest(request)
            .then((result: ng.IHttpPromiseCallbackArg<void>) => {
                return;
            });
    }

    public deleteVariantRequestById(id: string): ng.IPromise<void> {
        return this.variantApiService.deleteVariantRequestById(id)
            .then((result: ng.IHttpPromiseCallbackArg<void>) => {
                return;
            });
    }

    public getVariantRequestsByOwnerId(ownerId: string, assetGroupId?: string): ng.IPromise<VariantRequest[]> {
        return this.variantApiService.getVariantRequestsByOwnerId(ownerId, assetGroupId)
            .then((response: ng.IHttpPromiseCallbackArg<VariantRequest[]>) => {
                return response.data;
            });
    }

    public getVariantRequestById(id: string): ng.IPromise<VariantRequest> {
        return this.variantApiService.getVariantRequestById(id)
            .then((response: ng.IHttpPromiseCallbackArg<VariantRequest>) => {
                return response.data;
            });
    }

    public unlinkVariant(assetGroupId: string, variantId: string, eTag: string): ng.IPromise<AssetGroup> {
        return this.variantApiService.unlinkVariant(assetGroupId, variantId, eTag)
            .then((response: ng.IHttpPromiseCallbackArg<AssetGroup>) => {
                return response.data;
            });
    }
}
