import { Component } from "../../../module/app.module";
import template = require("./owner-view-drawer-trigger.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

const useCmsHere_DrawerTriggerLabel = "Team";

@Component({
    name: "pcdOwnerViewDrawerTrigger",
    options: {
        template,
        bindings: {
            owner: "<pcdOwner"
        }
    }
})
export default class OwnerViewDrawerTriggerComponent implements ng.IComponentController {
    // Input
    public owner: Pdms.DataOwner;

    public drawerTriggerLabel = useCmsHere_DrawerTriggerLabel;
}
