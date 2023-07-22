import * as angular from "angular";
import { Config, Inject } from "../../module/app.module";

import * as ManualRequest from "../manual-requests/manual-request-types";
import { IManualRequestsDataService } from "./manual-requests-data.service";
import { IMocksService } from "../mocks.service";
import { RequestStatus } from "../manual-requests/manual-request-types";
import { AppConfig } from "../../module/data.module";

class ManualRequestsDataMockService implements IManualRequestsDataService {
    @Config()
    @Inject("$provide")
    public static configureManualRequestsDataMockService($provide: ng.auto.IProvideService): void {

        $provide.decorator("manualRequestsDataService", ["$delegate", "$q", "mocksService",
            (
                $delegate: IManualRequestsDataService,
                $q: ng.IQService,
                mocksService: IMocksService,
            ): IManualRequestsDataService => {
                return mocksService.isActive() ? new ManualRequestsDataMockService(
                    $delegate,
                    $q,
                    mocksService,
                ) : $delegate;
            }
        ]);
    }

    constructor(
        private readonly real: IManualRequestsDataService,
        private readonly $promises: ng.IQService,
        private readonly mocksService: IMocksService,
    ) {
        console.debug("Using mocked ManualRequests service.");
    }

    public getDeleteDataTypesOnSubjectRequests(): ng.IPromise<any> {
        return this.real.getDeleteDataTypesOnSubjectRequests();
    }

    public getExportDataTypesOnSubjectRequests(): ng.IPromise<any> {
        return this.real.getExportDataTypesOnSubjectRequests();
    }

    public delete(subject: ManualRequest.PrivacySubjectIdentifier, metadata: ManualRequest.ManualRequestMetadata): ng.IPromise<any> {
        return this.real.delete(subject, metadata);
    }

    public export(subject: ManualRequest.PrivacySubjectIdentifier, metadata: ManualRequest.ManualRequestMetadata): ng.IPromise<any> {
        return this.real.export(subject, metadata);
    }

    public hasAccessForManualRequests(): ng.IPromise<any> {
        return this.real.hasAccessForManualRequests();
    }

    public getRequestStatuses(type: ManualRequest.PrivacyRequestType): ng.IPromise<RequestStatus[]> {
        return this.real.getRequestStatuses(type);
    }
}
