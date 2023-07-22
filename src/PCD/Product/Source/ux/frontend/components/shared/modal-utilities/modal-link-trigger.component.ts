import { Component, Inject } from "../../../module/app.module";
import template = require("./modal-link-trigger.html!text");

@Component({
    name: "pcdModalLinkTrigger",
    options: {
        bindings: {
            modalData: "<pcdModalData",
            label: "@pcdLabel",
            modalStateName: "@pcdModalStateName"
        },
        template,
    }
})
@Inject("$meeModal")
export default class ModalLinkTriggerComponent implements ng.IComponentController {
    /** 
     * Input: Modal data to set. 
     **/
    public modalData: string;
    /** 
     * Input: Text to show as the trigger. 
     **/
    public label: string;
    /** 
     * Input: UI state name for the modal to display. 
     **/
    public modalStateName: string;

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public triggerModal(): void {
        this.$meeModal.show("#modal-dialog", this.modalStateName, {
            data: this.modalData
        });
    }
}
