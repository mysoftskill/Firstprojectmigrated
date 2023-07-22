import { Component, Inject } from "../../../../module/app.module";
import template = require("./asset-transfer-requests.html!text");
import * as Pdms from "../../../../shared/pdms/pdms-types";
import { RequestContainer } from "../management-flows";

interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Data owner ID. 
     **/
    ownerId: string;
}

const useCmsHere_CollapsedToggleLabel = "Expand";
const useCmsHere_ExpandedToggleLabel = "Collapse";


@Component({
    name: "pcdAssetTransferRequests",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$stateParams", "$meeModal", "$q")
export default class AssetTransferRequestsComponent implements ng.IComponentController {
    public requestContainers: RequestContainer[] = [];
    public ownerId: string;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $stateParams: StateParams,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly $q: ng.IQService) { }

    public $onInit(): ng.IPromise<any> {
        this.ownerId = this.$stateParams.ownerId;
        return this.loadAssetTransferRequests();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("loadingAssetTransferRequests")
    public loadAssetTransferRequests(): ng.IPromise<any> {
        // Empty old asset transfer requests.
        this.requestContainers = [];

        return this.pdmsDataService.getTransferRequestsByTargetOwnerId(this.ownerId)
            .then((transferRequests: Pdms.TransferRequest[]) => {
                transferRequests.forEach((request: Pdms.TransferRequest) => {
                    this.requestContainers.push({
                        request: request,
                        isChecked: false,
                        isCollapsed: transferRequests.length > 3,
                        ownerViewModalTriggerData: {
                            ownerId: request.sourceOwnerId
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

    public hasTransferRequests(): boolean {
        return this.requestContainers && !!this.requestContainers.length;
    }

    public hasCheckedRequest(): boolean {
        return this.requestContainers.some(container => container.isChecked);
    }

    public approveAssetTransferRequests(): void {
        this.$meeModal.show("#modal-dialog", ".approve", {
            data: {
                requests: this.getCheckedAssetTransferRequests(),
                returnLocation: this.getReturnLocation(),
                onConfirm: () => this.onAccept()
            }
        });
    }

    public denyAssetTransferRequests(): void {
        this.$meeModal.show("#modal-dialog", ".deny", {
            data: {
                requests: this.getCheckedAssetTransferRequests(),
                returnLocation: this.getReturnLocation(),
                onConfirm: () => this.onDeny()
            }
        });
    }

    public onAccept(): ng.IPromise<any> {
        let selectedAssetTransferRequestIds = this.getCheckedAssetTransferRequests().map(requestContainer => {
            return requestContainer.request.id;
        });

        return this.pdmsDataService.approveTransferRequests(selectedAssetTransferRequestIds).finally(() =>
            this.loadAssetTransferRequests()
        );
    }

    public onDeny(): ng.IPromise<any> {
        let selectedAssetTransferRequestIds = this.getCheckedAssetTransferRequests().map(requestContainer => {
            return requestContainer.request.id;
        });

        return this.pdmsDataService.denyTransferRequests(selectedAssetTransferRequestIds).finally(() =>
            this.loadAssetTransferRequests()
        );
    }

    private getReturnLocation(): string {
        return (this.getUncheckedAssetTransferRequests().length > 0) ? "^" : "landing";
    }

    private getCheckedAssetTransferRequests(): RequestContainer[] {
        return this.requestContainers.filter(container => container.isChecked);
    }

    private getUncheckedAssetTransferRequests(): RequestContainer[] {
        return this.requestContainers.filter(container => !container.isChecked);
    }
}
