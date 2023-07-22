import { BreadcrumbNavigation } from "../../shared/breadcrumb-heading/breadcrumb-heading.component";

const useCmsHere_ManageDataAssetsPageHeading = "Data assets";
const useCmsHere_VariantsPageHeading = "Data asset variants";
const useCmsHere_VariantRequestsPageHeading = "Variant requests for data asset";

export const ManageDataAssetsPageBreadcrumb: BreadcrumbNavigation = {
    headingText: useCmsHere_ManageDataAssetsPageHeading,
    state: "data-assets.manage",
};

export const VariantPageBreadcrumb: BreadcrumbNavigation = {
    headingText: useCmsHere_VariantsPageHeading,
    state: "data-assets.manage.variants",
};

export const VariantRequestsPageBreadcrumb: BreadcrumbNavigation = {
    headingText: useCmsHere_VariantRequestsPageHeading,
    state: "data-assets.manage.variant-requests",
};
