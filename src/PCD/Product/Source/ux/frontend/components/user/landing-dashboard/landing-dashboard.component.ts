import { Component, Inject, Route } from "../../../module/app.module";
import template = require("./landing-dashboard.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { IOwnerIdContextService } from "../../../shared/owner-id-context.service";
import { TeamPickerModel } from "./team-picker.component";
import { DataOwnerHelper } from "../../../shared/data-owner-helper";

@Route({
    name: "landing",
    options: {
        url: "/",
        template: "<pcd-landing></pcd-landing>",
    }
})
@Component({
    name: "pcdLanding",
    options: {
        template
    }
})
@Inject("ownerIdContextService", "pdmsDataService", "$q")
export default class LandingDashboardComponent implements ng.IComponentController {
    public teamPickerModel: TeamPickerModel;
    public assetGroupsCount: number;
    public agentsCount: number;
    public selectedOwner: Pdms.DataOwner;

    constructor(
        private readonly ownerIdContextService: IOwnerIdContextService,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $q: ng.IQService) {
    }

    public $onInit(): ng.IPromise<any> {
        this.teamPickerModel = {
            selectedOwnerId: "",
            owners: []
        };

        return this.getOwners().then(() => {
            if (this.hasDataOwners()) {
                return this.onTeamSelected();
            }
        });
    }

    public requiresOwnerEdit(): boolean {
        return this.hasDataOwners() && !this.isValidDataOwner();
    }

    public shouldDisableManageSection(): boolean {
        return !this.hasDataOwners() || !this.isValidDataOwner();
    }

    public isValidDataOwner(): boolean {
        let selectedOwner = _.find(this.teamPickerModel.owners, (o: Pdms.DataOwner) => o.id === this.teamPickerModel.selectedOwnerId);

        return DataOwnerHelper.isValidDataOwner(selectedOwner);
    }

    public hasDataOwners(): boolean {
        return this.teamPickerModel.owners && this.teamPickerModel.owners.length > 0;
    }

    public hasPendingTransferRequests(): boolean {
        return this.selectedOwner && this.selectedOwner.hasPendingTransferRequests;
    }

    public hasDataAgents(): boolean {
        return !!this.agentsCount;
    }

    public hasAssetGroups(): boolean {
        return !!this.assetGroupsCount;
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchDataOwners")
    private getOwners(): ng.IPromise<any> {
        return this.pdmsDataService.getOwnersByAuthenticatedUser()
            .then((owners: Pdms.DataOwner[]) => {
                this.teamPickerModel = {
                    owners,
                    selectedOwnerId: ""
                };

                if (this.hasDataOwners()) {
                    let activeOwnerId = this.ownerIdContextService.getActiveOwnerId();
                    // Check if the active owner ID exists in the list, otherwise default to the first owner.
                    let expectedActiveOwner = _.find(this.teamPickerModel.owners, (o: Pdms.DataOwner) => o.id === activeOwnerId);
                    this.teamPickerModel.selectedOwnerId = (expectedActiveOwner && expectedActiveOwner.id) || this.teamPickerModel.owners[0].id;
                }
            });
    }

    public onTeamSelected(): ng.IPromise<any> {
        this.ownerIdContextService.setActiveOwnerId(this.teamPickerModel.selectedOwnerId);
        this.selectedOwner = _.find(this.teamPickerModel.owners, (owner: Pdms.DataOwner) =>
                owner.id === this.teamPickerModel.selectedOwnerId
            );

        return this.$q.all([
            this.getDataAssetsCountForOwner(),
            this.getDataAgentsCountForOwner()
        ]);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchDataAssetsCountForOwner")
    public getDataAssetsCountForOwner(): ng.IPromise<void> {
        return this.pdmsDataService.getAssetGroupsCountByOwnerId(this.teamPickerModel.selectedOwnerId)
            .then((count: number) => {
                this.assetGroupsCount = count;
            });
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchDataAgentsCountForOwner")
    public getDataAgentsCountForOwner(): ng.IPromise<void> {
        return this.pdmsDataService.getDataAgentsCountByOwnerId(this.teamPickerModel.selectedOwnerId)
            .then((count: number) => {
                this.agentsCount = count;
            });
    }
}
