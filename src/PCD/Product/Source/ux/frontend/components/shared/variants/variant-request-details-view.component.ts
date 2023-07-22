import { Component } from "../../../module/app.module";
import template = require("./variant-request-details-view.html!text");

import * as Variant from "../../../shared/variant/variant-types";

@Component({
    name: "pcdVariantRequestDetailsView",
    options: {
        template,
        bindings: {
            request: "<pcdVariantRequest"
        }
    }
})
export default class VariantRequestDetailsViewComponent implements ng.IComponentController {
    public request: Variant.VariantRequest;
}
