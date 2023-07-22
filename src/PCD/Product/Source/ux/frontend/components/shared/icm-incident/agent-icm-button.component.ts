import { Component, Inject } from "../../../module/app.module";
import template = require("./agent-icm-button.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { AgentIcmConfirmationModalData } from "./route-config";
import { IStringFormatFilter } from "../../../shared/filters/string-format.filter";

const useCmsHere_NoIcmConnectorIdTitle = "There is no IcM connector ID, please contact team {0} to set an IcM connector ID to file IcM incidents.";
const doNotLoc_IcmTitle = "Data agent export failed validation";
const doNotLoc_IcmBody = "";

// Keywords are needed on IcM to query these incidents.
const doNotLoc_IcmKeywords = "NgpExportError";

@Component({
    name: "pcdAgentIcmButton",
    options: {
        template,
        bindings: {
            agent: "<",
            owner: "<"
        }
    }
})
@Inject("pdmsDataService", "$meeModal", "stringFormatFilter")
export default class AgentIcmButtonComponent implements ng.IComponentController {
    public agent: Pdms.DataAgent;
    public owner: Pdms.DataOwner;
    public isAuthorized: boolean;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly stringFormatFilter: IStringFormatFilter) { }

    public $onInit(): ng.IPromise<any> {
        return this.pdmsDataService.hasAccessForIncidentManager()
                 .then(() => {
                     this.isAuthorized = true;
                 })
                 .catch(() => {
                     this.isAuthorized = false;
                 });
    }

    public onClick(): void {
        let data: AgentIcmConfirmationModalData = {
            owner: this.owner,
            incident: null,
            onConfirm: () => this.onCreateIcm()
        };
        this.$meeModal.show("#modal-dialog", ".icm", { data });
    }

    private onCreateIcm(): ng.IPromise<Pdms.Incident> {
        let incident: Pdms.Incident = {
            routing: {
                ownerId: this.agent.ownerId,
                agentId: this.agent.id
            },
            severity: Pdms.IcmIncidentSeverity.Sev4,
            title: doNotLoc_IcmTitle,
            body: doNotLoc_IcmBody,
            keywords: doNotLoc_IcmKeywords
        };

        return this.pdmsDataService.createIcmIncident(incident);
    }

    public hasIcmConnectorId(): boolean {
        return !!(this.agent && this.agent.icmConnectorId) || !!(this.owner && this.owner.icmConnectorId);
    }

    public getButtonTitle(): string {
        if (!this.hasIcmConnectorId() && this.owner) {
            return this.stringFormatFilter(useCmsHere_NoIcmConnectorIdTitle, [this.owner.name]);
        }

        return "";
    }
}
