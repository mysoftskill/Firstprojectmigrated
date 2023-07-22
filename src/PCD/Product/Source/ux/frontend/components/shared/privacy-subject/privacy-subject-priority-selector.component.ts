import { Component, Inject } from "../../../module/app.module";
import template = require("./privacy-subject-priority-selector.html!text");

import * as ManualRequest from "./../../../shared/manual-requests/manual-request-types";
import * as SelectList from "../../../shared/select-list";

const useCmsHere_High = "High";
const useCmsHere_Regular = "Regular";

@Component({
    name: "pcdPrivacySubjectPrioritySelector",
    options: {
        template,
        bindings: {
            model: "=pcdPriority"
        }
    }
})
export default class PrivacySubjectPrioritySelector implements ng.IComponentController {
    public priorityPickerModel: SelectList.Model;
    public model: string;

    public $onInit(): void {
        this.priorityPickerModel = {
            selectedId: this.model,
            items: [{
                id: ManualRequest.PrivacySubjectPriority[ManualRequest.PrivacySubjectPriority.Regular],
                label: useCmsHere_Regular
            }, {
                id: ManualRequest.PrivacySubjectPriority[ManualRequest.PrivacySubjectPriority.High],
                label: useCmsHere_High
            }]
        };
        SelectList.enforceModelConstraints(this.priorityPickerModel);
    }
}