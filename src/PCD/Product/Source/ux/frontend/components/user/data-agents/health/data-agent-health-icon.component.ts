import { Component } from "../../../../module/app.module";
import template = require("./data-agent-health-icon.html!text");
import { HealthIcon } from "../../../../shared/registration-status/registration-status-types";

export type HealthCheckCategory =
    "subject-type-tags-status" |
    "data-type-tags-status" |
    "assets-status" |
    "agent-health" |
    "protocols" |
    "environments" |
    "capabilities";

@Component({
    name: "pcdDataAgentHealthIcon",
    options: {
        template,
        bindings: {
            healthIcon: "<",
            category: "@"
        }
    }
})
export default class DataAgentHealthIcon implements ng.IComponentController {
    //  Inputs
    public healthIcon: HealthIcon;
    public category: HealthCheckCategory;

    public getStatusClass(): string[] {
        switch (this.healthIcon) {
            case HealthIcon.pending:
                return ["mee-icon", "mee-icon-History"];
            case HealthIcon.healthy:
                return ["mee-icon", "mee-icon-CompletedSolid"];
            case HealthIcon.unhealthy:
                return ["mee-icon", "mee-icon-StatusErrorFull"];
            case HealthIcon.incomplete:
                return ["mee-icon", "mee-icon-StatusErrorFull", "mild"];
            case HealthIcon.error:
                return ["mee-icon", "mee-icon-Sad"];
            case HealthIcon.unknown:
                return ["mee-icon", "mee-icon-WhatsThis"];
            default:
                return ["mee-icon", "mee-icon-History"];
        }
    }
}
