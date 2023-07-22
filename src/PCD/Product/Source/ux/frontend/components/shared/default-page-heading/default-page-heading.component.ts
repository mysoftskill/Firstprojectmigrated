import { Component } from "../../../module/app.module";
import template = require("./default-page-heading.html!text");

@Component({
    name: "pcdDefaultPageHeading",
    options: {
        bindings: {
            text: "@",
        },
        template,
    }
})
export default class DefaultPageHeading implements ng.IComponentController {
}
