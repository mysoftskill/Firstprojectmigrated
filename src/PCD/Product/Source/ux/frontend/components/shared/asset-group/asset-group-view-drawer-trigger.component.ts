import { Component } from "../../../module/app.module";
import template = require("./asset-group-view-drawer-trigger.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

const useCmsHere_DrawerTriggerLabel = "AssetGroup";

@Component({
    name: "pcdAssetGroupViewDrawerTrigger",
    options: {
        template,
        bindings: {
            assetGroup: "<pcdAssetGroup"
        }
    }
})
export default class AssetGroupViewDrawerTriggerComponent implements ng.IComponentController {
    // Input
    public assetGroup: Pdms.AssetGroup;

    public drawerTriggerLabel = useCmsHere_DrawerTriggerLabel;
}
