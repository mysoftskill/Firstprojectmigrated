import { Component, Inject } from "../../../module/app.module";
import template = require("./service-tree-summary.html!text");

import { IServiceTreeDataService } from "../../../shared/service-tree-data.service";
import * as Pdms from "../../../shared/pdms/pdms-types";

@Component({
    name: "pcdServiceTreeSummary",
    options: {
        bindings: {
            stService: "<pcdService",
        },
        template,
    }
})
@Inject("serviceTreeDataService")
export default class ServiceTreeSummary implements ng.IComponentController {
    public stService: Pdms.STServiceDetails;

    constructor(
        private readonly serviceTreeDataService: IServiceTreeDataService) {
    }

    public getServiceTreeLink(): string {
        return this.serviceTreeDataService.getServiceURL(this.stService);
    }

    public commaSeparateAdminSecurityGroups(): string {
        return this.stService && this.stService.serviceAdmins && this.stService.serviceAdmins.join(", ");
    }
}
