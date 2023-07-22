import { Component } from "../../../module/app.module";
import template = require("./edit-asset-group-properties.html!text");
import * as Pdms from "../../../shared/pdms/pdms-types";

@Component({
    name: "pcdEditAssetGroupProperties",
    options: {
        template,
        bindings: {
            asset: "<pcdAssetGroup"
        }
    }
})
export class EditAssetGroupPropertiesComponent implements ng.IComponentController {
    public asset: Pdms.AssetGroup;
}
