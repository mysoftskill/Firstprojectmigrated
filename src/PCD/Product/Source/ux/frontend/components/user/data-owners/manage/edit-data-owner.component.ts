import { Component, Inject, Route } from "../../../../module/app.module";
import template = require("./edit-data-owner.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { EditOwnerStateParams } from "./../management-flows";

const useCmsHere_PageHeading = "Edit team";

@Component({
    name: "pcdEditDataOwner",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$meeModal", "$state", "$stateParams")
export default class EditDataOwnerComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
    public owner: Pdms.DataOwner;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly $state: ng.ui.IStateService,
        private readonly $stateParams: EditOwnerStateParams) { }

    public $onInit(): void {
        this.owner = this.$stateParams.dataOwner;
    }

    public showLinkToServiceTreeModalDialog(): void {
        this.$meeModal.setData(this.owner);
        this.$meeModal.show("#modal-dialog", ".link-service-tree");
    }

    public saveClicked(updatedOwner: Pdms.DataOwner): void {
        this.$state.go("landing");
    }
}
