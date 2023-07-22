import * as angular from "angular";
import { TestSpec, SpyCache } from "../../shared-tests/spec.base";

import * as ManualRequest from "../manual-requests/manual-request-types";
import { IManualRequestsApiService } from "./manual-requests-api.service";
import { IManualRequestsDataService } from "./manual-requests-data.service";

describe("Manual requests data service", () => {
    let spec: TestSpec;
    let manualRequestsDataService: IManualRequestsDataService;
    let manualRequestsApiServiceMock: SpyCache<IManualRequestsApiService>;
    let $q: ng.IQService;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_manualRequestsApiService_: IManualRequestsApiService, _manualRequestsDataService_: IManualRequestsDataService, _$q_: ng.IQService) => {
            manualRequestsApiServiceMock = new SpyCache(_manualRequestsApiService_);
            manualRequestsDataService = _manualRequestsDataService_;
            $q = _$q_;
        });
    });

    it("sends manual delete request to API service for a demographic subject", (done: DoneFn) => {
        // arrange
        let subject: ManualRequest.DemographicSubject = {
            kind: "Demographic",
            names: ["someValue"],
            emails: ["someValue"],
            phoneNumbers: ["someValue"],
            postalAddress: {
                streetNumbers: ["someValue"],
                streetNames: ["someValue"],
                unitNumbers: ["someValue"],
                cities: ["someValue"],
                regions: ["someValue"],
                postalCodes: ["someValue"],
            }
        };
        let metadata: ManualRequest.ManualRequestMetadata = {
            capId: "someValue",
            countryOfResidence: "someValue",
            priority: "Regular"
        };
        manualRequestsApiServiceMock.getFor("deleteDemographicSubjectRequest").and.returnValue(spec.asHttpPromise<any>({}));

        // act
        manualRequestsDataService.delete(subject, metadata)
            .then(() => {

                // assert
                done();
            });
        spec.runDigestCycle();
    });

    it("gets command statuses", () => {
        // arrange
        let result: ManualRequest.RequestStatus[] = [];
        manualRequestsApiServiceMock.getFor("getRequestStatuses").and.returnValue(spec.asHttpPromise(result));

        // act
        manualRequestsDataService.getRequestStatuses(ManualRequest.PrivacyRequestType.Export);
        spec.runDigestCycle();

        // assert
        expect(manualRequestsApiServiceMock.getFor("getRequestStatuses")).toHaveBeenCalled();
    });
});
