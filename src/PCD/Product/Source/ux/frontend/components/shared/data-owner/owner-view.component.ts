import { Component, Inject } from "../../../module/app.module";
import template = require("./owner-view.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { IServiceTreeDataService } from "../../../shared/service-tree-data.service";

@Component({
    name: "pcdOwnerView",
    options: {
        template,
        bindings: {
            owner: "<pcdOwner",
            hideTitle: "<?pcdHideTitle"
        }
    }
})
@Inject("serviceTreeDataService", "$state")
export default class OwnerViewComponent implements ng.IComponentController {
    //  Input
    public owner: Pdms.DataOwner;
    public hideTitle: boolean;

    public description: string[] = [];

    constructor(
        private readonly serviceTreeDataService: IServiceTreeDataService,
        private readonly $state: ng.ui.IStateService) { }

    public isServiceTreeDataOwner(): boolean {
        return !!this.owner.serviceTree;
    }

    public getServiceTreeLink(): string {
        if (this.isServiceTreeDataOwner()) {
            return this.serviceTreeDataService.getServiceURL(this.owner.serviceTree);
        }

        return "";
    }

    public getViewLink(): string {
        return this.$state.href("data-owners.view", { ownerId: this.owner.id }, { absolute: true });
    }

    public $onChanges(changes: ng.IOnChangesObject): void {
        if (this.owner && this.owner.description) {
            this.description = this.owner.description.split("\n");
        }
    }
}
