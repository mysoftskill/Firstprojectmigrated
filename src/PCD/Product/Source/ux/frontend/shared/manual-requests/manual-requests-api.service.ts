import { Service, Inject } from "../../module/app.module";
import { AppConfig } from "../../module/data.module";

import * as ManualRequest from "../manual-requests/manual-request-types";
import { IMsalTokenManagerFactory } from "../msal-token-manager";
import { IAjaxService, IAjaxServiceFactory, IAjaxServiceOptions } from "../ajax.service";

export interface IManualRequestsApiService {
    //  Checks if the user has access for manual requests.
    hasAccessForManualRequests(): ng.IHttpPromise<any>;

    //  Gets the data types for the operation.
    getDeleteDataTypesOnSubjectRequests(): ng.IHttpPromise<any>;

    //  Gets the data types for the operation.
    getExportDataTypesOnSubjectRequests(): ng.IHttpPromise<any>;

    //  Manually deletes data for the demographic subject.
    deleteDemographicSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any>;

    //  Manually deletes data for the Microsoft employee subject.
    deleteMicrosoftEmployeeSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any>;

    //  Manually deletes data for the MSA self auth subject.
    deleteMsaSelfAuthSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any>;

    //  Manually exports data for the demographic subject.
    exportDemographicSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any>;

    //  Manually exports data for the Microsoft employee subject.
    exportMicrosoftEmployeeSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any>;

    //  Manually exports data for the MSA self auth subject.
    exportMsaSelfAuthSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any>;

    //  Gets requests statuses
    getRequestStatuses(type: ManualRequest.PrivacyRequestType): ng.IHttpPromise<any>;
}

@Service({
    name: "manualRequestsApiService"
})
@Inject("appConfig", "msalTokenManagerFactory", "ajaxServiceFactory", "$q")
class ManualRequestsApiService implements IManualRequestsApiService {
    private ajaxService: IAjaxService;

    constructor(
        private readonly appConfig: AppConfig,
        private readonly msalTokenManagerFactory: IMsalTokenManagerFactory,
        private readonly ajaxServiceFactory: IAjaxServiceFactory,
        private readonly $q: ng.IQService
    ) {
        let ajaxOptions: IAjaxServiceOptions = {
            authTokenManager: msalTokenManagerFactory.createInstance(this.appConfig.azureAdAppId)
        };
        this.ajaxService = ajaxServiceFactory.createInstance(ajaxOptions);
    }

    public hasAccessForManualRequests(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/manual-request/api/hasaccess",
            serviceName: "PdmsUx",
            operationName: "HasAccess"
        });
    }

    public getDeleteDataTypesOnSubjectRequests(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/manual-request/api/getdeletedatatypesonsubjectrequests",
            serviceName: "PdmsUx",
            operationName: "GetDeleteDataTypesOnSubjectRequests",
            cache: true
        });
    }

    public getExportDataTypesOnSubjectRequests(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/manual-request/api/getexportdatatypesonsubjectrequests",
            serviceName: "PdmsUx",
            operationName: "GetExportDataTypesOnSubjectRequests",
            cache: true
        });
    }

    public deleteDemographicSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/manual-request/api/deletedemographicsubjectrequest",
            serviceName: "PdmsUx",
            operationName: "DeleteDemographicSubjectRequest",
            data: manualRequest
        });
    }

    public deleteMicrosoftEmployeeSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/manual-request/api/deletemicrosoftemployeesubjectrequest",
            serviceName: "PdmsUx",
            operationName: "DeleteMicrosoftEmployeeSubjectRequest",
            data: manualRequest
        });
    }

    public deleteMsaSelfAuthSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/manual-request/api/deletemsaselfauthsubjectrequest",
            serviceName: "PdmsUx",
            operationName: "DeleteMsaSelfAuthSubjectRequest",
            data: manualRequest
        });
    }

    public exportDemographicSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/manual-request/api/exportdemographicsubjectrequest",
            serviceName: "PdmsUx",
            operationName: "ExportDemographicSubjectRequest",
            data: manualRequest
        });
    }

    public exportMicrosoftEmployeeSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/manual-request/api/exportmicrosoftemployeesubjectrequest",
            serviceName: "PdmsUx",
            operationName: "ExportMicrosoftEmployeeSubjectRequest",
            data: manualRequest
        });
    }

    public exportMsaSelfAuthSubjectRequest(manualRequest: ManualRequest.ManualRequestEntry): ng.IHttpPromise<any> {
        return this.ajaxService.post({
            url: "/manual-request/api/exportmsaselfauthsubjectrequest",
            serviceName: "PdmsUx",
            operationName: "ExportMsaSelfAuthSubjectRequest",
            data: manualRequest
        });
    }

    public getRequestStatuses(type: ManualRequest.PrivacyRequestType): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/manual-request/api/getrequeststatuses",
            serviceName: "PdmsUx",
            operationName: "GetRequestStatuses",
            customDataCallBack: (jqXhr: JQueryXHR): any => {
                return {
                    items: (jqXhr && jqXhr.responseJSON && jqXhr.responseJSON.length) || 0,
                    type: type
                };
            },
            data: { type }
        });
    }
}
