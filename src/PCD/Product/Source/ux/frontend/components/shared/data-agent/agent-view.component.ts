import { Component, Inject } from "../../../module/app.module";
import template = require("./agent-view.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

@Component({
    name: "pcdAgentView",
    options: {
        template,
        bindings: {
            agent: "<pcdAgent",
            hideTitle: "<?pcdHideTitle"
        }
    }
})
@Inject("$state")
class DataAgentViewComponent implements ng.IComponentController {
    //  Input
    public agent: Pdms.DataAgent;
    public hideTitle: boolean;

    public description: string[] = [];

    constructor(
        private readonly $state: ng.ui.IStateService) {
    }

    public $onChanges(changes: ng.IOnChangesObject): void {
        if (this.agent && this.agent.description) {
            this.description = this.agent.description.split("\n");
        }
    }

    public getViewLink(): string {
        return this.$state.href("data-agents.view", { agentId: this.agent.id }, { absolute: true });
    }
}
