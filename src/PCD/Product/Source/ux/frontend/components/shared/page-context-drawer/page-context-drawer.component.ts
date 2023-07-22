import { Component } from "../../../module/app.module";
import template = require("./page-context-drawer.html!text");

@Component({
    name: "pcdPageContextDrawer",
    options: {
        template,
        transclude: {
            trigger: "pcdDrawerTrigger",
            content: "pcdDrawerContent"
        }
    }
})
export default class PageContextDrawerComponent implements ng.IComponentController {
}
