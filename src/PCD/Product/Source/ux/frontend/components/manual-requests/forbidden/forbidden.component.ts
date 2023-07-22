import { Component, Inject } from "../../../module/app.module";
import template = require("./forbidden.html!text");
import { AppConfig } from "../../../module/data.module";

const useCmsHere_PageHeading = "Forbidden";

@Inject("appConfig")
@Component({
    name: "pcdManualRequestsForbidden",
    options: {
        template
    }
})
export default class ManualRequestsForbidden implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;

    constructor(
        private readonly appConfig: AppConfig) { }

    public isPartnerTestEnvironment(): boolean {
        return this.appConfig.environmentType === "int";
    }
}
