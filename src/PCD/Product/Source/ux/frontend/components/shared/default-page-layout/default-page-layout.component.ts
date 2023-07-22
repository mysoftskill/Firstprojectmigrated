import { Component } from "../../../module/app.module";
import template = require("./default-page-layout.html!text");

@Component({
    name: "pcdDefaultPageLayout",
    options: {
        template,
        transclude: {
            "heading": "pcdHeading",
            "sidebar": "?pcdSidebar",
            "lede": "?pcdLede",
            "content": "pcdContent",
        },
    }
})
export default class DefaultPageLayout implements ng.IComponentController {
}
