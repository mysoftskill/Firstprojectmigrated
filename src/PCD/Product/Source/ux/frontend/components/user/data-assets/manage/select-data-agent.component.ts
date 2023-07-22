import { Component, Inject } from "../../../../module/app.module";
import template = require("./select-data-agent.html!text");

import * as ManagementFlows from "../management-flows";
import * as Pdms from "../../../../shared/pdms/pdms-types";

import { StateParams } from "../management-flows";
import { DataAgentLinkingContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";
import * as SelectList from "../../../../shared/select-list";
import { IGroundControlApiService } from "../../../../shared/flighting/ground-control-api.service";

const useCmsHere_SharedSuffix = " (Shared)";
const errorCategory = "manage-data-assets.select-agent";
const useCmsHere_FailedToLoadAgents = "Failed to load data agents. Please close dialog and try again.";
const useCmsHere_CheckBoxError = "This checkbox selection is mandatory.";
const useCmsHere_CheckBoxFeatureFlag = "PCD.EUDB.SelfAttest";

@Component({
    name: "pcdSelectDataAgent",
    options: {
        template,
        bindings: {
        }
    }
})
@Inject("pdmsDataService", "$state", "$q", "$stateParams", "$meeModal", "pcdErrorService", "groundControlApiService")
export default class SelectDataAgent implements ng.IComponentController {

    public context: DataAgentLinkingContext;
    public errorCategory = errorCategory;
    public selfAttestCheckbox: boolean;
    public dataAgentListPickerModel: SelectList.Model = {
        selectedId: null,
        items: []
    };
    private userFlights: string[];

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $q: ng.IQService,
        private readonly $stateParams: StateParams,
        private readonly $modalState: MeePortal.OneUI.Angular.IModalStateService,
        private readonly pcdError: IPcdErrorService,
        private readonly groundControlApiService: IGroundControlApiService
        ) { }

    public $onInit() {
        this.context = this.$modalState.getData<DataAgentLinkingContext>();
        this.pcdError.resetErrorsForCategory(this.errorCategory);
        this.getLinkableDataAgents();
        this.getGroundControl();
    }

    private getGroundControl(): ng.IPromise<void> {
        return this.groundControlApiService.getUserFlights().then(userFlights => {
            this.userFlights = userFlights.data;
        });
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("loadDataAgents")
    private getLinkableDataAgents(): ng.IPromise<void> {
        return this.pdmsDataService.getSharedDataAgentsByOwnerId(this.$stateParams.ownerId)
        .then((dataAgents) => {
            this.dataAgentListPickerModel.items = dataAgents.map(da => {
                return {
                    id: da.id,
                    label: this.getAgentName(da)
                };
            });

            if (this.dataAgentListPickerModel.items[0]) {
                this.dataAgentListPickerModel.selectedId = this.dataAgentListPickerModel.items[0].id;
            }
        })
        .catch(() => {
            this.pcdError.setErrorForId(`${this.errorCategory}.agent-id`, useCmsHere_FailedToLoadAgents);
        });
    }

    public cancel(): void {
        this.$modalState.hide("^");
    }

    public isCheckBoxRequired(): boolean {
        return this.userFlights && this.userFlights.includes(useCmsHere_CheckBoxFeatureFlag);
    }

    public next(): void {
        if(!this.selfAttestCheckbox) {
            this.pcdError.setErrorForId(`${this.errorCategory}.checkbox`, useCmsHere_CheckBoxError);
            return;
        }

        this.context.agentId = this.dataAgentListPickerModel.selectedId;

        this.context.agentName = this.dataAgentListPickerModel.items.filter(
            i => i.id === this.dataAgentListPickerModel.selectedId
        )[0].label;

        this.$modalState.switchTo("^.link-data-agent");
    }

    public getAgentName(dataAgent: Pdms.DataAgent): string {
        return dataAgent.name + (dataAgent.sharingEnabled ? useCmsHere_SharedSuffix : "");
    }
}
