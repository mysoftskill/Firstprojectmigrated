import * as angular from "angular";

import { Route, Inject, appModule } from "../../module/app.module";
import { IManualRequestsDataService } from "../../shared/manual-requests/manual-requests-data.service";
import { BreadcrumbNavigation } from "../shared/breadcrumb-heading/breadcrumb-heading.component";

// Common state parameters for a completed manual request.
export interface CompletedRequestStateParams extends ng.ui.IStateParamsService {
    capId: string;
    requestIds: string[];
}

const useCmsHere_ManualRequestsLandingPageHeading = "PRC requests";

// Breadcrumbs to use in the routes.
export const ManualRequestsLandingPageBreadcrumb: BreadcrumbNavigation = {
    headingText: useCmsHere_ManualRequestsLandingPageHeading,
    state: "manual-requests"
};

//  This callback is required to restrict access to Manual Requests.
let onRouteEnterCallback = ["manualRequestsDataService", "$state",
    (manualRequestsData: IManualRequestsDataService, $state: ng.ui.IStateService) => {
        let navigateToForbiddenPage = () => {
            console.warn("User is not authorized to issue manual requests.");

            //  Navigate the user to an insufficient permissions page.
            $state.go("manual-requests-forbidden");
        };

        return manualRequestsData.hasAccessForManualRequests()
                .catch(() => {
                    //  If the call fails due to 403 or some other transient error, navigate to forbidden page.
                    navigateToForbiddenPage();
                });
    }];

@Route({
    name: "manual-requests-forbidden",
    options: {
        url: "/manual-requests/forbidden",
        template: "<ui-view><pcd-manual-requests-forbidden></pcd-manual-requests-forbidden></ui-view>",
    }
})
@Route({
    name: "manual-requests",
    options: {
        url: "/manual-requests",
        template: "<ui-view><pcd-manual-requests-home></pcd-manual-requests-home></ui-view>",
        onEnter: onRouteEnterCallback,
    }
})
@Route({
    name: "manual-requests.delete",
    options: {
        url: "/delete",
        template: "<ui-view><pcd-manual-requests-delete></pcd-manual-requests-delete></ui-view>",
    }
})
@Route({
    name: "manual-requests.delete.request-completed",
    options: {
        template: "<pcd-request-completed></pcd-request-completed>",
        params: <CompletedRequestStateParams> {
            capId: null,
            requestIds: null
        },
    }
})
@Route({
    name: "manual-requests.export",
    options: {
        url: "/export",
        template: "<ui-view><pcd-manual-requests-export></pcd-manual-requests-export></ui-view>",
    }
})
@Route({
    name: "manual-requests.export.request-completed",
    options: {
        template: "<pcd-request-completed></pcd-request-completed>",
        params: <CompletedRequestStateParams>{
            capId: null,
            requestIds: null
        },
    }
})
@Route({
    name: "manual-requests.status",
    options: {
        url: "/status",
        template: "<pcd-manual-requests-status></pcd-manual-requests-status>",
    }
})
export class __registerManualRequestsRoutes { }
