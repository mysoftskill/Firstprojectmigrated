import { Component } from "../../../module/app.module";
import template = require("./breadcrumb-heading.html!text");

//  The navigation information to use in the breadcrumb.
export interface BreadcrumbNavigation {
    //  Breadcrumb title to display.
    headingText: string;
    //  Breadcrumb state to transition to.
    state: string;
}

@Component({
    name: "pcdBreadcrumbHeading",
    options: {
        bindings: {
            text: "@",
            breadcrumbs: "<?pcdBreadcrumbs"
        },
        template,
    }
})
export default class BreadcrumbHeadingComponent implements ng.IComponentController {
    //  Input: Title text describing the current page.
    public text: string;

    /**
     * Input: Navigation hierarchy used to display the breadcrumb trail.
     * Pages are in order of site level (i.e., [Parent, Section, Child]), excluding "Home" and the current page.
     */
    public breadcrumbs: BreadcrumbNavigation[];
}
