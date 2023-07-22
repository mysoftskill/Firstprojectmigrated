import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { IAjaxService } from "../ajax.service";
import { IVariantApiService } from "./variant-api.service";
import { VariantRequest } from "./variant-types";

describe("Variant API service", () => {
    let variantApiService: IVariantApiService;
    let ajaxServiceMock: SpyCache<IAjaxService>;

    beforeEach(() => {
        let spec = new TestSpec({
            returnMockedAjaxService: true,
            mockedAjaxServiceOptions: {
                authTokenManager: null
            }
        });
        ajaxServiceMock = spec.ajaxServiceMock;

        inject((_variantApiService_: IVariantApiService) => {
            variantApiService = _variantApiService_;
        });
    });

    it("can get variants", () => {
        // arrange
        ajaxServiceMock.getFor("get").and.stub();

        // act
        variantApiService.getVariants();

        // assert
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "variant/api/getvariants",
            serviceName: "PdmsUx",
            operationName: "GetVariants"
        });
    });

    it("can create variant request", () => {
        // arrange
        let fakeRequest: VariantRequest = {
            id: null,
            ownerId: "owner1",
            ownerName: null,
            requestedVariants: [],
            variantRelationships: [],
            trackingDetails: null
        };

        ajaxServiceMock.getFor("post").and.stub();

        // act
        variantApiService.createVariantRequest(fakeRequest);

        // assert
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "variant/api/createvariantrequest",
            serviceName: "PdmsUx",
            operationName: "CreateVariantRequest",
            data: fakeRequest
        });
    });

    describe("gets variant requests", () => {
        beforeEach(() => {
            ajaxServiceMock.getFor("get").and.stub();
        });

        it("by owner ID and asset group ID", () => {
            variantApiService.getVariantRequestsByOwnerId("2c22345gh-8f454-12345678", "1234567890");
            expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
                url: "variant/api/getvariantrequestsbyownerid",
                serviceName: "PdmsUx",
                operationName: "GetVariantRequestsByOwnerId",
                data: { ownerId: "2c22345gh-8f454-12345678", assetGroupId: "1234567890" }
            });
        });

        it("ONLY by owner ID", () => {
            variantApiService.getVariantRequestsByOwnerId("2c22345gh-8f454-12345678");
            expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
                url: "variant/api/getvariantrequestsbyownerid",
                serviceName: "PdmsUx",
                operationName: "GetVariantRequestsByOwnerId",
                data: { ownerId: "2c22345gh-8f454-12345678", assetGroupId: undefined }
            });
        });
    });

    it("get a variant request by ID", () => {
        ajaxServiceMock.getFor("get").and.stub();
        variantApiService.getVariantRequestById("2c22345gh-ehds7-12345678");
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "variant/api/getvariantrequestbyid",
            serviceName: "PdmsUx",
            operationName: "GetVariantRequestById",
            data: { id: "2c22345gh-ehds7-12345678" }
        });
    });

    it("delete a variant request by ID", () => {
        ajaxServiceMock.getFor("del").and.stub();
        variantApiService.deleteVariantRequestById("2c22345gh-ehds7-12345678");
        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "variant/api/deletevariantrequestbyid",
            serviceName: "PdmsUx",
            operationName: "DeleteVariantRequestById",
            data: { id: "2c22345gh-ehds7-12345678" }
        });
    });

    it("unlinks a variant from an asset group", () => {
        ajaxServiceMock.getFor("del").and.stub();
        variantApiService.unlinkVariant("2c22345gh-ehds7-12345678", "2c22345gh-ehds7-09876543", "2c22345gh-a3ds7-12345678");
        expect(ajaxServiceMock.getFor("del")).toHaveBeenCalledWith({
            url: "variant/api/unlinkvariant",
            serviceName: "PdmsUx",
            operationName: "UnlinkVariant",
            data: {
                assetGroupId: "2c22345gh-ehds7-12345678",
                variantId: "2c22345gh-ehds7-09876543",
                eTag: "2c22345gh-a3ds7-12345678"
            }
        });
    });
});
