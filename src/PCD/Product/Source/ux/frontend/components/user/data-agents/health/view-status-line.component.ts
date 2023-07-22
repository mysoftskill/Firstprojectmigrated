import { Component } from "../../../../module/app.module";
import template = require("./view-status-line.html!text");
import { RegistrationState, HealthIcon } from "../../../../shared/registration-status/registration-status-types";
import { HealthCheckCategory } from "./data-agent-health-icon.component";
import { HealthFilterType } from "./view-agents-health.component";

interface SpecialIconRule {
    category: HealthCheckCategory;
    state: RegistrationState;
    iconState: HealthIcon;
}

@Component({
    name: "pcdViewStatusLine",
    options: {
        template,
        transclude: {
            inline: "?pcdInline",
            description: "?pcdDescription",
            body: "?pcdBody"
        },
        bindings: {
            displayByStatus: "@",
            stateLabel: "@",
            iconState: "<?",
            category: "@",
            state: "<?",
            showDrawer: "<?",
            drawerState: "@"
        }
    }
})
export default class ViewStatusLineComponent implements ng.IComponentController {
    // Inputs
    public stateLabel: string;
    public state?: RegistrationState;
    public category: HealthCheckCategory;
    public iconState?: HealthIcon;
    public displayByStatus: HealthFilterType;
    public showDrawer: boolean;
    public drawerState: string;

    private specialIconRules: SpecialIconRule[] = [
        {
            category: "subject-type-tags-status",
            state: RegistrationState.notApplicable,
            iconState: HealthIcon.healthy
        },
        {
            category: "data-type-tags-status",
            state: RegistrationState.notApplicable,
            iconState: HealthIcon.healthy
        },
        {
            category: "environments",
            state: RegistrationState.partial,
            iconState: HealthIcon.incomplete
        }
    ];

    public isNotValid(): boolean {
        return !!this.state && this.state !== RegistrationState.valid;
    }

    public shouldDisplayStatus(): boolean {
        return (this.displayByStatus === "issues" && this.getStatusIcon() !== HealthIcon.healthy)
            || this.displayByStatus !== "issues";
    }

    public getStatusIcon(): HealthIcon {
        if (this.iconState) {
            return this.iconState;
        }

        // if any of category, registrationState combination have special rules on whats healthy/unhealthy
        // use this lookup table or else general rules apply in the switch statement.
        let specialIconState = _.chain(this.specialIconRules)
            .filter((ss: SpecialIconRule) => ss.category === this.category && ss.state === this.state)
            .map((ss: SpecialIconRule) => ss.iconState)
            .first()
            .value();

        if (specialIconState) {
            return specialIconState;
        }

        // general icon rules
        switch (this.state) {
            case RegistrationState.valid:
                return HealthIcon.healthy;
            case RegistrationState.validButTruncated:
                return HealthIcon.incomplete;
            default:
                return HealthIcon.unhealthy;
        }
    }
}
