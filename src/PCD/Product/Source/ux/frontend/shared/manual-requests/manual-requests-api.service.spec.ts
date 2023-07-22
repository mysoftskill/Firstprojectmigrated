import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { IAjaxService } from "../ajax.service";
import { IManualRequestsApiService } from "./manual-requests-api.service";

import * as ManualRequest from "../manual-requests/manual-request-types";
import "./manual-requests-api.service";

describe("Manual requests API service", () => {
    let manualRequestsApiService: IManualRequestsApiService;
    let ajaxServiceMock: SpyCache<IAjaxService>;

    beforeEach(() => {
        let spec = new TestSpec({
            returnMockedAjaxService: true,
            mockedAjaxServiceOptions: {
                authTokenManager: null
            }
        });
        ajaxServiceMock = spec.ajaxServiceMock;

        inject((_manualRequestsApiService_: IManualRequestsApiService) => {
            manualRequestsApiService = _manualRequestsApiService_;
        });
    });

    it("sends manual delete request with required parameters for demographic subjects", () => {
        // arrange
        let data: ManualRequest.ManualRequestEntry = { subject: null, metadata: null };
        ajaxServiceMock.getFor("post").and.stub();

        // act
        manualRequestsApiService.deleteDemographicSubjectRequest(data);

        // assert
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/manual-request/api/deletedemographicsubjectrequest",
            serviceName: "PdmsUx",
            operationName: "DeleteDemographicSubjectRequest",
            data: data
        });
    });

    it("sends manual delete request with required parameters for Microsoft employee subjects", () => {
        // arrange
        let data: ManualRequest.ManualRequestEntry = { subject: null, metadata: null };
        ajaxServiceMock.getFor("post").and.stub();

        // act
        manualRequestsApiService.deleteMicrosoftEmployeeSubjectRequest(data);

        // assert
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/manual-request/api/deletemicrosoftemployeesubjectrequest",
            serviceName: "PdmsUx",
            operationName: "DeleteMicrosoftEmployeeSubjectRequest",
            data: data
        });
    });

    it("sends manual delete request with required parameters for MSA self auth subjects", () => {
        // arrange
        let data: ManualRequest.ManualRequestEntry = { subject: null, metadata: null };
        ajaxServiceMock.getFor("post").and.stub();

        // act
        manualRequestsApiService.deleteMsaSelfAuthSubjectRequest(data);

        // assert
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/manual-request/api/deletemsaselfauthsubjectrequest",
            serviceName: "PdmsUx",
            operationName: "DeleteMsaSelfAuthSubjectRequest",
            data: data
        });
    });

    it("sends manual export request with required parameters for demographic subjects", () => {
        // arrange
        let data: ManualRequest.ManualRequestEntry = { subject: null, metadata: null };
        ajaxServiceMock.getFor("post").and.stub();

        // act
        manualRequestsApiService.exportDemographicSubjectRequest(data);

        // assert
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/manual-request/api/exportdemographicsubjectrequest",
            serviceName: "PdmsUx",
            operationName: "ExportDemographicSubjectRequest",
            data: data
        });
    });

    it("sends manual export request with required parameters for Microsoft employee subjects", () => {
        // arrange
        let data: ManualRequest.ManualRequestEntry = { subject: null, metadata: null };
        ajaxServiceMock.getFor("post").and.stub();

        // act
        manualRequestsApiService.exportMicrosoftEmployeeSubjectRequest(data);

        // assert
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/manual-request/api/exportmicrosoftemployeesubjectrequest",
            serviceName: "PdmsUx",
            operationName: "ExportMicrosoftEmployeeSubjectRequest",
            data: data
        });
    });

    it("sends manual export request with required parameters for MSA self auth subjects", () => {
        // arrange
        let data: ManualRequest.ManualRequestEntry = { subject: null, metadata: null };
        ajaxServiceMock.getFor("post").and.stub();

        // act
        manualRequestsApiService.exportMsaSelfAuthSubjectRequest(data);

        // assert
        expect(ajaxServiceMock.getFor("post")).toHaveBeenCalledWith({
            url: "/manual-request/api/exportmsaselfauthsubjectrequest",
            serviceName: "PdmsUx",
            operationName: "ExportMsaSelfAuthSubjectRequest",
            data: data
        });
    });

    it("gets command statuses", () => {
        ajaxServiceMock.getFor("get").and.stub();
        manualRequestsApiService.getRequestStatuses(ManualRequest.PrivacyRequestType.Export);
        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/manual-request/api/getrequeststatuses",
            serviceName: "PdmsUx",
            operationName: "GetRequestStatuses",
            customDataCallBack: jasmine.any(Function),
            data: { type: ManualRequest.PrivacyRequestType.Export }
        });
    });

    it("reports # of request status records, based on the service response", () => {
        ajaxServiceMock.getFor("get").and.stub();
        manualRequestsApiService.getRequestStatuses(ManualRequest.PrivacyRequestType.Export);

        let ajaxOptions: Bradbury.JQueryTelemetryAjaxSettings = ajaxServiceMock.getFor("get").calls.mostRecent().args[0];

        let jqXhrWithoutJson: Partial<JQueryXHR> = {};
        let jqXhrWithoutArray: Partial<JQueryXHR> = {
            responseJSON: {}
        };
        let jqXhrWithArray: Partial<JQueryXHR> = {
            responseJSON: [123, 456, 789]
        };

        expect(ajaxOptions.customDataCallBack(null)).toEqual({ items: 0, type: ManualRequest.PrivacyRequestType.Export });
        expect(ajaxOptions.customDataCallBack(<JQueryXHR> jqXhrWithoutJson)).toEqual({ items: 0, type: ManualRequest.PrivacyRequestType.Export });
        expect(ajaxOptions.customDataCallBack(<JQueryXHR> jqXhrWithoutArray)).toEqual({ items: 0, type: ManualRequest.PrivacyRequestType.Export });
        expect(ajaxOptions.customDataCallBack(<JQueryXHR> jqXhrWithArray)).toEqual({ items: 3, type: ManualRequest.PrivacyRequestType.Export });
    });

    it("gets access for manual requests correctly", () => {
        ajaxServiceMock.getFor("get").and.stub();

        manualRequestsApiService.hasAccessForManualRequests();

        expect(ajaxServiceMock.getFor("get")).toHaveBeenCalledWith({
            url: "/manual-request/api/hasaccess",
            serviceName: "PdmsUx",
            operationName: "HasAccess",
        });
    });
});
