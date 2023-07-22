import { Component, Inject } from "../../../../module/app.module";
import template = require("./manage-data-assets.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageDataAgentsPageBreadcrumb } from "../breadcrumbs-config";

const useCmsHere_PageHeading = "Data assets linked to data agent";

interface StateParams extends ng.ui.IStateParamsService {
    /**
     *  Data agent ID. 
     **/
    agentId: string;

    /**
     *  Owner ID. 
     **/
    ownerId: string;
}

@Component({
    name: "pcdManageDataAgentAssets",
    options: {
        template
    }
})
@Inject("$stateParams", "pdmsDataService", "$q")
export default class ManageDataAssetsComponent implements ng.IComponentController {
    public pageHeading = useCmsHere_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAgentsPageBreadcrumb];

    public assetGroups: Pdms.AssetGroup[];
    public dataAgent: Pdms.DataAgent;
    public ownerId: string;

    constructor(
        private readonly $stateParams: StateParams,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $q: ng.IQService) {
    }

    public $onInit(): ng.IPromise<any> {
        this.ownerId = this.$stateParams.ownerId;
        return this.getData();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchDataForAgent")
    public getData(): ng.IPromise<any> {
        return this.$q.all([
            this.getAgent(),
            this.getDataAssets()
        ]);
    }

    private getDataAssets(): ng.IPromise<void> {
        return this.pdmsDataService.getAssetGroupsByAgentId(this.$stateParams.agentId)
            .then((assetGroups: Pdms.AssetGroup[]) => {
                this.assetGroups = assetGroups;
            });
    }

    private getAgent(): ng.IPromise<void> {
        if (this.dataAgent) {
            return this.$q.resolve();
        }

        return this.pdmsDataService.getDeleteAgentById(this.$stateParams.agentId)
            .then((dataAgent: Pdms.DataAgent) => {
                this.dataAgent = dataAgent;
            });
    }
}
