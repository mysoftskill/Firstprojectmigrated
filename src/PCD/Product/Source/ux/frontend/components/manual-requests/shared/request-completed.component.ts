import { Component, Inject } from "../../../module/app.module";
import template = require("./request-completed.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { CompletedRequestStateParams } from "../route-config";
import { BreadcrumbNavigation } from "../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManualRequestsLandingPageBreadcrumb } from "../route-config";

const useCmsHere_PageHeading = "Tracking information";

@Component({
    name: "pcdRequestCompleted",
    options: {
        template
    }
})
@Inject("$stateParams")
export default class RequestCompletedComponent implements ng.IComponentController {
    public capId: string;
    public resultingRequestIds: string[];

    public pageHeading: string = useCmsHere_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManualRequestsLandingPageBreadcrumb];

    constructor(
        private readonly $stateParams: CompletedRequestStateParams
    ) {
        this.resultingRequestIds = this.$stateParams.requestIds;
        this.capId = this.$stateParams.capId;
    }
}
