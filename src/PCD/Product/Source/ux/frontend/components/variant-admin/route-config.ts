import { Route } from "../../module/app.module";
import { IVariantAdminDataService } from "../../shared/variant-admin/variant-admin-data.service";
import { BreadcrumbNavigation } from "../shared/breadcrumb-heading/breadcrumb-heading.component";

const useCmsHere_ManageVariantRequestsPageHeading = "Manage variant requests";

/** 
 * Breadcrumbs to use in the routes. 
 **/
export const ManageVariantRequestsPageBreadcrumb: BreadcrumbNavigation = {
    headingText: useCmsHere_ManageVariantRequestsPageHeading,
    state: "variant-admin"
};

// This callback is required to restrict access to Variant Admin.
let onRouteEnterCallback = ["variantAdminDataService", "$state",
    (variantAdminData: IVariantAdminDataService, $state: ng.ui.IStateService) => {
        return variantAdminData.hasAccessForVariantAdmin()
                 .catch(() => {
                     console.warn("User is not authorized to issue variant admin operations.");

                     // Navigate the user to an insufficient permissions page.
                     $state.go("variant-admin-forbidden");
                 });
    }];

@Route({
    name: "variant-admin-forbidden",
    options: {
        url: "/admin/variants/forbidden",
        template: "<ui-view><pcd-variant-admin-forbidden></pcd-variant-admin-forbidden></ui-view>",
    }
})
@Route({
    name: "variant-admin",
    options: {
        url: "/admin/variants",
        template: "<ui-view><pcd-manage-variant-requests></pcd-manage-variant-requests></ui-view>",
        onEnter: onRouteEnterCallback,
    }
})
@Route({
    name: "variant-admin.request-details",
    options: {
        url: "/details/{variantRequestId:guid}",
        template: "<pcd-admin-variant-request-details></pcd-admin-variant-request-details>",
        onEnter: onRouteEnterCallback,
        params: {
            request: null
        }
    }
})
export class __registerVariantAdminRoutes { }
