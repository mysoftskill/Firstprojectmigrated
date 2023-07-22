import { Component, Inject, Route } from "../../../module/app.module";
import template = require("./landing-pivot.html!text");

@Component({
    name: "pcdLandingPivot",
    options: {
        template
    }
})
export default class LandingPivotComponent implements ng.IComponentController {
}
