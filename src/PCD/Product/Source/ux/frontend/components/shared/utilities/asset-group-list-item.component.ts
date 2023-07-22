import { Component, Inject } from "../../../module/app.module";
import template = require("./asset-group-list-item.html!text");

@Component({
    name: "assetGroupListItem",
    options: {
        bindings: {
            ngAriaLabel: "@assetListItemAriaLabel"
        },
        require: { ngModelCtrl: "^ngModel" },
        template
    }
})
@Inject("$meeModal")
export class AssetGroupListItemComponent implements ng.IComponentController {
    public readonly errorCategory = "checkbox";
    public ngAriaLabel: string;
    private ngModelCtrl: ng.INgModelController;
    public ngModel: boolean;
    
    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public onCheckBoxComponentChange(): void {
        this.ngModelCtrl.$setViewValue(this.ngModel);
    }

}
