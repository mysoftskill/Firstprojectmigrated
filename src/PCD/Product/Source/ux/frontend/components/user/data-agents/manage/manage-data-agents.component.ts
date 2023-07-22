import { Component, Inject } from "../../../../module/app.module";
import template = require("./manage-data-agents.html!text");
import { StateParams } from "../management-flows";

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { DeleteDataAgentConfirmationModalData } from "../../../shared/data-agent/delete-data-agent-confirmation.component";
import { IContactService } from "../../../../shared/contact.service";

const useCmsHere_PageHeading = "Data agents";

@Component({
    name: "pcdManageDataAgents",
    options: {
        template
    }
})
@Inject("$stateParams", "pdmsDataService", "$meeModal", "contactService")
export default class ManageDataAgentsComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
    public dataAgents: Pdms.DataAgent[] = [];
    public ownerId: string;

    constructor(
        private readonly $stateParams: StateParams,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly contactService: IContactService) {
    }

    public $onInit(): ng.IPromise<void> {
        this.ownerId = this.$stateParams.ownerId;
        return this.getDataAgentsForOwner();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchDataAgentsForOwner")
    public getDataAgentsForOwner(): ng.IPromise<any> {
        return this.pdmsDataService.getDataAgentsByOwnerId(this.ownerId)
            .then((dataAgents: Pdms.DataAgent[]) => {
                this.dataAgents = dataAgents;
            });
    }

    public showDeleteAgentConfirmationDialog(dataAgent: Pdms.DataAgent): void {
        let data: DeleteDataAgentConfirmationModalData = {
            dataAgent,
            onConfirm: () => this.onDeleteAgent(dataAgent)
        };
        this.$meeModal.show("#modal-dialog", ".delete-data-agent", { data });
    }

    public requestMoveTeamAgents(): void {
        this.contactService.requestAdminAssistance("move-team-assets", {
            entityId: this.ownerId
        });
    }

    public agentWithSharingRequestsExist(): boolean {
        return _.any(this.dataAgents, (da: Pdms.DataAgent) => da.hasSharingRequests);
    }

    private onDeleteAgent(dataAgent: Pdms.DataAgent): ng.IPromise<any> {
        return this.pdmsDataService.deleteDataAgent(dataAgent)
            .then(() => {
                this.dataAgents.splice(this.dataAgents.indexOf(dataAgent), 1);
            });
    }
}
