import { Component, Inject } from "../../../../module/app.module";
import template = require("./privacy-actions-details.html!text");
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { StateParams } from "../management-flows";
import { ManageDataAssetsPageBreadcrumb } from "../breadcrumbs-config";

import * as Pdms from "../../../../shared/pdms/pdms-types";

const useCmsData_PageHeading = "Privacy actions";
const useCmsHere_DeleteLabel = "Delete / Account Close";
const useCmsHere_ExportLabel = "Export";
const useCmsHere_PendingDeleteLabel = "Pending Delete / Account Close";
const useCmsHere_PendingExportLabel = "Pending Export";

type PrivacyActionType = "Delete / Account Close" | "Export" | "Pending Delete / Account Close" | "Pending Export";

interface PrivacyActionParams extends StateParams {
    assetGroupId: string;
}

interface DisplayPrivacyActionDetail {
    type: PrivacyActionType;
    agent: Pdms.DeleteAgent;
    owner: Pdms.DataOwner;
}

@Component({
    name: "pcdPrivacyActionsDetails",
    options: {
        template
    }
})
@Inject("$stateParams", "pdmsDataService", "$q")
export default class PrivacyActionsDetailsComponent implements ng.IComponentController {
    public pageHeading = useCmsData_PageHeading;
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAssetsPageBreadcrumb];

    public privacyActionDetails: DisplayPrivacyActionDetail[] = [];
    public assetGroup: Pdms.AssetGroup;
    public assetGroupId: string;

    constructor(
        private readonly $stateParams: PrivacyActionParams,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $q: ng.IQService) {
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializePrivacyActionsDetails")
    public $onInit(): ng.IPromise<any> {
        this.assetGroupId = this.$stateParams.assetGroupId;
        return this.pdmsDataService.getAssetGroupById(this.assetGroupId)
            .then((assetGroup: Pdms.AssetGroup) => {
                this.assetGroup = assetGroup;
                return this.getPrivacyActionDetails();
            });

    }

    public hasPrivacyActionDetails(): boolean {
        return !!this.privacyActionDetails.length;
    }

    private getPrivacyActionDetails(): ng.IPromise<any> {
        let promises: ng.IPromise<any>[] = [];

        if (this.assetGroup.deleteAgentId) {
            promises.push(this.getPrivacyActionDetail(this.assetGroup.deleteAgentId, useCmsHere_DeleteLabel));
        }
        if (this.assetGroup.exportAgentId) {
            promises.push(this.getPrivacyActionDetail(this.assetGroup.exportAgentId, useCmsHere_ExportLabel));
        }
        if (this.assetGroup.deleteSharingRequestId) {
            promises.push(this.pdmsDataService.getSharingRequestById(this.assetGroup.deleteSharingRequestId)
                .then((sharingRequest: Pdms.SharingRequest) => {
                    return this.getPrivacyActionDetail(sharingRequest.agentId, useCmsHere_PendingDeleteLabel);

                })
            );
        }
        if (this.assetGroup.exportSharingRequestId) {
            promises.push(this.pdmsDataService.getSharingRequestById(this.assetGroup.exportSharingRequestId)
                .then((sharingRequest: Pdms.SharingRequest) => {
                    return this.getPrivacyActionDetail(sharingRequest.agentId, useCmsHere_PendingExportLabel);
                })
            );
        }

        return this.$q.all(promises);
    }

    private getPrivacyActionDetail(agentId: string, detailType: PrivacyActionType): ng.IPromise<any> {
        return this.pdmsDataService.getDeleteAgentById(agentId)
            .then((agent: Pdms.DeleteAgent) => {
                return this.pdmsDataService.getDataOwnerWithServiceTree(agent.ownerId)
                    .then((owner: Pdms.DataOwner) => {
                        this.privacyActionDetails.push({
                            type: detailType,
                            agent: agent,
                            owner: owner
                        });
                    });
            });
    }
}
