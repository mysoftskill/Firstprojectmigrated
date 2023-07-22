import { Component, Inject } from "../../../module/app.module";
import template = require("./asset-owner-indicator.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

export type DataOwnershipIndicator = "yes" | "no";

@Component({
    name: "pcdAssetOwnerIndicator",
    options: {
        template,
        bindings: {
            isDataOwner: "=ngModel"
        }
    }
})
@Inject("pdmsDataService", "$state", "$stateParams", "$q")
export default class AssetOwnerIndicatorComponent implements ng.IComponentController {
    /**
     *  Input: data ownership indicator. 
     **/
    public isDataOwner: DataOwnershipIndicator;

    constructor() { }
}
