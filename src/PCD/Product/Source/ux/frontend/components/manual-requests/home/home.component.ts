import { Component, Inject, Route } from "../../../module/app.module";
import template = require("./home.html!text");
import { ManualRequestsLandingPageBreadcrumb } from "../route-config";

@Component({
    name: "pcdManualRequestsHome",
    options: {
        template
    }
})
export default class ManualRequestsHome implements ng.IComponentController {
    public pageHeading = ManualRequestsLandingPageBreadcrumb.headingText;
 }
