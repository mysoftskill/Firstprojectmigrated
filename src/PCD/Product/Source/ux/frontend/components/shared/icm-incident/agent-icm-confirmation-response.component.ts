import { Component, Inject } from "../../../module/app.module";
import template = require("./agent-icm-confirmation-response.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { ConfirmationModalData } from "../utilities/confirmation-modal-actions.component";
import { AgentIcmConfirmationModalData } from "./route-config";
import { IStringFormatFilter } from "../../../shared/filters/string-format.filter";

const doNotLoc_IncidentUrl = "https://portal.microsofticm.com/imp/v3/incidents/details/{0}";

@Component({
    name: "pcdAgentIcmConfirmationResponse",
    options: {
        template
    }
})
@Inject("$meeModal", "stringFormatFilter")
export default class AgentIcmConfirmationResponseComponent implements ng.IComponentController {
    public incidentId: number;
    public incidentUrl: string;

    constructor(
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly stringFormatFilter: IStringFormatFilter) { }

    public $onInit(): void {
        const data = this.$meeModal.getData<AgentIcmConfirmationModalData>();

        this.incidentId = data.incident.id;
        this.incidentUrl = this.stringFormatFilter(doNotLoc_IncidentUrl, [this.incidentId]);
    }

    public close(): void {
        this.$meeModal.hide("^");
    }
}
