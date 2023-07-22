import { Component, Inject, Route } from "../../../../module/app.module";
import template = require("./pending-sharing-requests.html!text");
import * as Pdms from "../../../../shared/pdms/pdms-types";
import { StringUtilities } from "../../../../shared/string-utilities";
import { OwnerViewModalTriggerData } from "../../../shared/data-owner/owner-view-modal-trigger.component";
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { ManageDataAgentsPageBreadcrumb } from "../breadcrumbs-config";

interface StateParams extends ng.ui.IStateParamsService {
    /**
     *  Data agent ID. 
     **/
    agentId: string;

    /**
     *  Data owner ID. 
     **/
    ownerId: string;
}

export interface RequestContainer {
    request: Pdms.SharingRequest;
    isChecked: boolean;
    isCollapsed: boolean;
    ownerViewModalTriggerData: OwnerViewModalTriggerData;
}

const useCmsHere_NoPrivacyActions = "None";
const useCmsHere_CollapsedToggleLabel = "Expand";
const useCmsHere_ExpandedToggleLabel = "Collapse";
const useCmsHere_PrivacyActionStrings: { [privacyActionId: string]: string } = {
    Delete: "Delete / Account Close",
    Export: "Export"
};


@Component({
    name: "pcdPendingSharingRequests",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$stateParams", "$meeModal")
export default class PendingSharingRequestsComponent implements ng.IComponentController {
    public requestContainers: RequestContainer[] = [];
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAgentsPageBreadcrumb];
    public agentId: string;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $stateParams: StateParams,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): ng.IPromise<any> {
        this.agentId = this.$stateParams.agentId;
        return this.loadSharingRequests();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("loadingSharingRequests")
    public loadSharingRequests(): ng.IPromise<any> {
        // Empty old sharing requests.
        this.requestContainers = [];

        return this.pdmsDataService.getSharingRequestsByAgentId(this.agentId)
            .then((sharingRequests: Pdms.SharingRequest[]) => {
                sharingRequests.forEach((request: Pdms.SharingRequest) => {
                    this.requestContainers.push({
                        request: request,
                        isChecked: false,
                        isCollapsed: sharingRequests.length > 3,
                        ownerViewModalTriggerData: {
                            ownerId: request.ownerId
                        }
                    });
                });
            });
    }

    public getToggleLabel(isCollapsed: boolean): string {
        return isCollapsed ? useCmsHere_CollapsedToggleLabel : useCmsHere_ExpandedToggleLabel;
    }

    public toggleExpandCollapse(container: RequestContainer): void {
        container.isCollapsed = !container.isCollapsed;
    }

    public hasSharingRequests(): boolean {
        return this.requestContainers && !!this.requestContainers.length;
    }

    public hasCheckedRequest(): boolean {
        return this.requestContainers.some(container => container.isChecked);
    }

    public approveSharingRequests(): void {
        this.$meeModal.show("#modal-dialog", ".approve", {
            data: {
                requests: this.getCheckedSharingRequests(),
                onConfirm: () => this.onAccept()
            }
        });
    }

    public denySharingRequests(): void {
        this.$meeModal.show("#modal-dialog", ".deny", {
            data: {
                requests: this.getCheckedSharingRequests(),
                onConfirm: () => this.onDeny()
            }
        });
    }

    public getCapabilities(capabilities: string[]): string {
        return StringUtilities.getCommaSeparatedList(capabilities, useCmsHere_PrivacyActionStrings, useCmsHere_NoPrivacyActions);
    }

    private getCheckedSharingRequests(): RequestContainer[] {
        return this.requestContainers.filter(container => container.isChecked);
    }

    public onAccept(): ng.IPromise<any> {
        let selectedSharingRequestIds = this.getCheckedSharingRequests().map(requestContainer => {
            return requestContainer.request.id;
        });

        return this.pdmsDataService.approveSharingRequests(selectedSharingRequestIds).finally(() => this.loadSharingRequests());
    }

    public onDeny(): ng.IPromise<any> {
        let selectedSharingRequestIds = this.getCheckedSharingRequests().map(requestContainer => {
            return requestContainer.request.id;
        });

        return this.pdmsDataService.denySharingRequests(selectedSharingRequestIds).finally(() => this.loadSharingRequests());
    }
}
