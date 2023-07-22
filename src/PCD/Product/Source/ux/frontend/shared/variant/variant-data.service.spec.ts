import * as angular from "angular";
import { TestSpec, SpyCache } from "../../shared-tests/spec.base";

import { IVariantApiService } from "./variant-api.service";
import { IVariantDataService } from "./variant-data.service";
import { VariantRequest } from "./variant-types";

describe("Variant data service", () => {
    let spec: TestSpec;
    let variantDataService: IVariantDataService;
    let variantApiServiceMock: SpyCache<IVariantApiService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_variantApiService_: IVariantApiService, _variantDataService_: IVariantDataService) => {
            variantApiServiceMock = new SpyCache(_variantApiService_);
            variantDataService = _variantDataService_;
        });
    });

    it("gets variants", () => {
        // arrange
        let result: VariantRequest[] = [];
        variantApiServiceMock.getFor("getVariants").and.returnValue(spec.asHttpPromise(result));

        // act
        variantDataService.getVariants();
        spec.runDigestCycle();

        // assert
        expect(variantApiServiceMock.getFor("getVariants")).toHaveBeenCalled();
    });

    it("can create variant request", () => {
        // arrange
        let request: VariantRequest = {
            id: null,
            variantRelationships: [],
            requestedVariants: [],
            ownerName: null,
            ownerId: "owner1",
            trackingDetails: null
        };
        variantApiServiceMock.getFor("createVariantRequest").and.returnValue(spec.asHttpPromise<any>(null));

        // act
        variantDataService.createVariantRequest(request);
        spec.runDigestCycle();

        // assert
        expect(variantApiServiceMock.getFor("createVariantRequest")).toHaveBeenCalledWith(request);
    });

    describe("gets variant requests", () => {
        let requests: VariantRequest[] = [{
            id: "359603",
            ownerId: "4cb-44be-ac24-0114de4fcf07",
            ownerName: "OwnerName",
            trackingDetails: {
                createdOn: null
            },
            requestedVariants: [{
                variantId: "1234567890",
                variantName: "MockVariant1",
                tfsTrackingUris: ["www.test.com"],
                disabledSignalFiltering: false,
                variantExpiryDate: null,
                variantState: null
            }],
            variantRelationships: [{
                assetGroupId: "1234567890a",
                assetGroupQualifier: {
                    props: {
                        AssetType: "CosmosStructuredStream",
                        PhysicalCluster: "guess",
                        VirtualCluster: "mylove",
                        RelativePath: "/local/lul"
                    }
                }
            }]
        }, {
            id: "9274678",
            ownerId: "4cb-44be-ac24-0114de4fcf07",
            ownerName: "OwnerName2",
            trackingDetails: {
                createdOn: null
            },
            requestedVariants: [{
                variantId: "12345678901",
                variantName: "MockVariant1",
                tfsTrackingUris: ["www.test2.com"],
                disabledSignalFiltering: false,
                variantExpiryDate: null,
                variantState: null
            }],
            variantRelationships: [{
                assetGroupId: "1234567890a",
                assetGroupQualifier: {
                    props: {
                        AssetType: "CosmosStructuredStream",
                        PhysicalCluster: "guesses",
                        VirtualCluster: "myloves",
                        RelativePath: "/local/lulz"
                    }
                }
            }]
        }];

        beforeEach(() => {
            variantApiServiceMock.getFor("getVariantRequestsByOwnerId").and.returnValue(spec.asHttpPromise(requests));
        });

        it("by owner id and asset group id", () => {
            let retrievedFromApiCall: VariantRequest[];
            variantDataService.getVariantRequestsByOwnerId("4cb-44be-ac24-0114de4fcf07", "1234567890a").then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(requests);
        });

        it("ONLY by owner id", () => {
            let retrievedFromApiCall: VariantRequest[];
            variantDataService.getVariantRequestsByOwnerId("4cb-44be-ac24-0114de4fcf07").then(data => { retrievedFromApiCall = data; });
            spec.runDigestCycle();
            expect(retrievedFromApiCall).toEqual(requests);
        });

    });

    it("get variant request by id", () => {
        let request: VariantRequest = {
            id: "359603",
            ownerId: "4cb-44be-ac24-0114de4fcf07",
            ownerName: "OwnerName",
            trackingDetails: {
                createdOn: null
            },
            requestedVariants: [{
                variantId: "1234567890",
                variantName: "MockVariant1",
                tfsTrackingUris: ["www.test.com"],
                disabledSignalFiltering: false,
                variantExpiryDate: null,
                variantState: null
            }],
            variantRelationships: [{
                assetGroupId: "1234567890a",
                assetGroupQualifier: {
                    props: {
                        AssetType: "CosmosStructuredStream",
                        PhysicalCluster: "guess",
                        VirtualCluster: "mylove",
                        RelativePath: "/local/lul"
                    }
                }
            }]
        };
        variantApiServiceMock.getFor("getVariantRequestById").and.returnValue(spec.asHttpPromise(request));

        let retrievedFromApiCall: VariantRequest;
        variantDataService.getVariantRequestById("359603").then(data => { retrievedFromApiCall = data; });
        spec.runDigestCycle();
        expect(retrievedFromApiCall).toEqual(request);
    });

    it("delete variant request by id", () => {
        variantApiServiceMock.getFor("deleteVariantRequestById").and.returnValue(spec.asHttpPromise(null));

        variantDataService.deleteVariantRequestById("359603");
        spec.runDigestCycle();
        expect(variantApiServiceMock.getFor("deleteVariantRequestById")).toHaveBeenCalledWith("359603");
    });

    it("unlinks a variant from an asset group", () => {
        variantApiServiceMock.getFor("unlinkVariant").and.returnValue(spec.asHttpPromise(null));

        variantDataService.unlinkVariant("359603", "1234567", "12345678");
        spec.runDigestCycle();
        expect(variantApiServiceMock.getFor("unlinkVariant")).toHaveBeenCalledWith("359603", "1234567", "12345678");
    });
});
