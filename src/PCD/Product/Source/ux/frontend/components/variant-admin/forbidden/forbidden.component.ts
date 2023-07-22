import { Component } from "../../../module/app.module";
import template = require("./forbidden.html!text");

const useCmsHere_PageHeading = "Forbidden";

@Component({
    name: "pcdVariantAdminForbidden",
    options: {
        template
    }
})
export default class VariantAdminForbidden implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
}
