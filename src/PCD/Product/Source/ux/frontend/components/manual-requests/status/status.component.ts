import { Component, Inject } from "../../../module/app.module";
import template = require("./status.html!text");

import * as ManualRequest from "./../../../shared/manual-requests/manual-request-types";
import { IManualRequestsDataService } from "../../../shared/manual-requests/manual-requests-data.service";
import { BreadcrumbNavigation } from "../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManualRequestsLandingPageBreadcrumb } from "../route-config";
import { IAngularFailureResponse } from "../../../shared/ajax.service";
import { ErrorCodeHelper } from "../../shared/utilities/error-code-helper";
import * as SelectList from "../../../shared/select-list";

const useCmsHere_PageHeading = "Request status";
const useCmsHere_SearchPlaceholderLabel = "Search";
const useCmsHere_DisplayLabels = {
    CapId: "CapId",
    CountryOfResidence: "Country",
    ManualRequestSubmitter: "Submitter",
    Priority: "Priority",
    submitted: "Submitted",
    completed: "Completed",
    DemographicSubject: "Alternate subject",
    MsaSubject: "MSA",
    MicrosoftEmployee: "Microsoft employee",
    Unknown: "Unknown subject type",
    Export: "Show export requests",
    Delete: "Show delete requests",
    AccountClose: "Show account close requests"
};
const useCmsHere_PrivacyRequestSelectTypes = {
    Export: ManualRequest.PrivacyRequestType.Export,
    Delete: ManualRequest.PrivacyRequestType.Delete,
    AccountClose: ManualRequest.PrivacyRequestType.AccountClose
};

interface DisplayRequestStatus {
    context: any;
    destinationUri: string;
    id: string;
    state: string;
    subjectType: string;
    submittedTime: string;
    isCompleted?: boolean;
    completedTime?: string;
    progress?: string;
}

@Component({
    name: "pcdManualRequestsStatus",
    options: {
        template
    }
})
@Inject("manualRequestsDataService", "$state")
export default class ManualRequestsStatus implements ng.IComponentController {
    public pageHeading: string = useCmsHere_PageHeading;
    public searchPlaceholderLabel: string = useCmsHere_SearchPlaceholderLabel;
    public searchAriaLabel: string = useCmsHere_SearchPlaceholderLabel;
    public breadcrumbs: BreadcrumbNavigation[] = [ManualRequestsLandingPageBreadcrumb];
    private readonly dateTimeFormatter = new Intl.DateTimeFormat();
    public typeListPickerModel: SelectList.Model = {
        selectedId: null,
        items: []
    };

    public requestStatuses: DisplayRequestStatus[] = [];

    constructor(
        private readonly manualRequestsDataService: IManualRequestsDataService,
        private readonly $state: ng.ui.IStateService) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeRequestsStatus")
    public $onInit(): ng.IPromise<any> {
        this.typeListPickerModel.items = Object.keys(useCmsHere_PrivacyRequestSelectTypes).map(label => {
            return {
                id: label,
                label: this.getDisplayLabel(label)
            };
        });

        this.typeListPickerModel.selectedId = "Export";

        return this.loadStatuses();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchRequestsStatus")
    public loadStatuses(): ng.IPromise<any> {
        return this.manualRequestsDataService.getRequestStatuses(useCmsHere_PrivacyRequestSelectTypes[this.typeListPickerModel.selectedId])
            .then((statuses: ManualRequest.RequestStatus[]) => {
                this.requestStatuses = statuses.map(status => {
                    let result: DisplayRequestStatus = {
                        context: this.getContextObject(status.context),
                        destinationUri: status.destinationUri,
                        id: status.id,
                        state: this.getDisplayLabel(status.state),
                        subjectType: this.getDisplayLabel(status.subjectType),
                        submittedTime: this.dateTimeFormatter.format(new Date(status.submittedTime)),
                        progress: status.progress ? `${status.progress}%` : ""
                    };

                    if ("completed" === status.state) {
                        result.isCompleted = true;
                        result.completedTime = this.dateTimeFormatter.format(new Date(status.completedTime));
                    }

                    return result;
                });
            })
            .catch((e: IAngularFailureResponse) => {
                if (ErrorCodeHelper.getErrorCode(e) === "notAuthorized") {
                    // Navigate the user to an insufficient permissions page.
                    this.$state.go("manual-requests-forbidden");
                }
                throw e;
            });
    }

    public getContextObject(context: string): any {
        return JSON.parse(context);
    }

    public getDisplayLabel(label: string): string {
        return useCmsHere_DisplayLabels[label] || label;
    }

    public hasRequestStatuses(): boolean {
        return this.requestStatuses.length > 0;
    }
}
