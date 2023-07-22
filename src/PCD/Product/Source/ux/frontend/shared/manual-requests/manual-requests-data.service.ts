import * as angular from "angular";
import { Service, Inject } from "../../module/app.module";

import * as Shared from "../shared-types";
import * as ManualRequest from "../manual-requests/manual-request-types";
import { IManualRequestsApiService } from "./manual-requests-api.service";

export interface IManualRequestsDataService {
    //  Gets the data types for the operation.
    getDeleteDataTypesOnSubjectRequests(): ng.IPromise<ManualRequest.DataTypesOnSubjectRequest>;

    //  Gets the data types for the operation.
    getExportDataTypesOnSubjectRequests(): ng.IPromise<ManualRequest.DataTypesOnSubjectRequest>;

    //  Manually deletes data for the privacy subject.
    delete(subjectData: ManualRequest.PrivacySubjectIdentifier, metadata: ManualRequest.ManualRequestMetadata): ng.IPromise<ManualRequest.OperationResponse>;

    //  Manually exports data for the privacy subject.
    export(subjectData: ManualRequest.PrivacySubjectIdentifier, metadata: ManualRequest.ManualRequestMetadata): ng.IPromise<ManualRequest.OperationResponse>;

    //  Determines if authenticated user is authorized to perform manual requests.
    hasAccessForManualRequests(): ng.IPromise<any>;

    //   Gets requests statuses
    getRequestStatuses(type: ManualRequest.PrivacyRequestType): ng.IPromise<ManualRequest.RequestStatus[]>;
}

@Service({
    name: "manualRequestsDataService"
})
@Inject("$q", "manualRequestsApiService")
class ManualRequestsDataService implements IManualRequestsDataService {
    constructor(
        private $q: ng.IQService,
        private manualRequestsApiService: IManualRequestsApiService) { }

    public getDeleteDataTypesOnSubjectRequests(): ng.IPromise<any> {
        return this.manualRequestsApiService.getDeleteDataTypesOnSubjectRequests()
            .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.DataTypesOnSubjectRequest>) => {
                return response.data;
            });
    }

    public getExportDataTypesOnSubjectRequests(): ng.IPromise<any> {
        return this.manualRequestsApiService.getExportDataTypesOnSubjectRequests()
            .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.DataTypesOnSubjectRequest>) => {
                return response.data;
            });
    }

    public delete(subject: ManualRequest.PrivacySubjectIdentifier, metadata: ManualRequest.ManualRequestMetadata): ng.IPromise<ManualRequest.OperationResponse> {
        switch (subject.kind) {
            case "Demographic":
                return this.manualRequestsApiService.deleteDemographicSubjectRequest({ subject, metadata })
                    .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.OperationResponse>) => {
                        return response.data;
                    });

            case "MicrosoftEmployee":
                return this.manualRequestsApiService.deleteMicrosoftEmployeeSubjectRequest({ subject, metadata })
                    .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.OperationResponse>) => {
                        return response.data;
                    });

            case "MSA":
                return this.manualRequestsApiService.deleteMsaSelfAuthSubjectRequest({ subject, metadata })
                    .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.OperationResponse>) => {
                        return response.data;
                    });

            default:
                return Shared.invalidConditionBreakBuild(subject);
        }
    }

    public export(subject: ManualRequest.PrivacySubjectIdentifier, metadata: ManualRequest.ManualRequestMetadata): ng.IPromise<ManualRequest.OperationResponse> {
        switch (subject.kind) {
            case "Demographic":
                return this.manualRequestsApiService.exportDemographicSubjectRequest({ subject, metadata })
                    .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.OperationResponse>) => {
                        return response.data;
                    });

            case "MicrosoftEmployee":
                return this.manualRequestsApiService.exportMicrosoftEmployeeSubjectRequest({ subject, metadata })
                    .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.OperationResponse>) => {
                        return response.data;
                    });

            case "MSA":
                return this.manualRequestsApiService.exportMsaSelfAuthSubjectRequest({ subject, metadata })
                    .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.OperationResponse>) => {
                        return response.data;
                    });

            default:
                return Shared.invalidConditionBreakBuild(subject);
        }
    }

    public hasAccessForManualRequests(): ng.IPromise<any> {
        return this.manualRequestsApiService.hasAccessForManualRequests();
    }

    public getRequestStatuses(type: ManualRequest.PrivacyRequestType): ng.IPromise<ManualRequest.RequestStatus[]> {
        return this.manualRequestsApiService.getRequestStatuses(type)
            .then((response: ng.IHttpPromiseCallbackArg<ManualRequest.RequestStatus[]>) => {
                    return response.data;
                });
        
    }
}
