import { Component } from "../../../module/app.module";
import template = require("./agent-view-drawer-trigger.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

const useCmsHere_DrawerTriggerLabel = "Agent";

@Component({
    name: "pcdAgentViewDrawerTrigger",
    options: {
        template,
        bindings: {
            agent: "<pcdAgent"
        }
    }
})
export default class AgentViewDrawerTriggerComponent implements ng.IComponentController {
    //  Input
    public agent: Pdms.DataAgent;

    public drawerTriggerLabel = useCmsHere_DrawerTriggerLabel;
}
