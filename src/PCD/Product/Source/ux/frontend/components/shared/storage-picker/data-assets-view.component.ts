import { Component, Inject } from "../../../module/app.module";
import template = require("./data-assets-view.html!text");
import * as Pdms from "../../../shared/pdms/pdms-types";

const useCmsHere_DefaultCaption = "List of data assets";
const useCmsHere_DefaultNoDataAssetsLabel = "No data assets";

export type WarningStyle = "none" | "simple" | "custom1";

@Component({
    name: "pcdDataAssetsView",
    options: {
        template,
        transclude: {
            "simpleWarning": "?pcdSimpleWarning",
            "customWarning1": "?pcdCustomWarning1"
        },
        bindings: {
            caption: "@pcdCaption",
            noDataAssetsLabel: "@pcdNoDataAssetsLabel",
            ngModel: "<",
            warningStyle: "@pcdWarningStyle"
        },
    }
})
export class DataAssetsViewComponent implements ng.IComponentController {
    public ngModel: Pdms.DataAsset[];
    public caption = useCmsHere_DefaultCaption;
    public noDataAssetsLabel = useCmsHere_DefaultNoDataAssetsLabel;
    public warningStyle: WarningStyle = "none";

    public hasDataAssets(): boolean {
        return !!this.ngModel && !!this.ngModel.length;
    }
}
