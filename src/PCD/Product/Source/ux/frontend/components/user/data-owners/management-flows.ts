import { Route } from "../../../module/app.module";

import "./create/service-tree/create-team.component";
import "./view/view-data-owner.component";
import "./manage/owner-editor.component";
import "./manage/edit-data-owner-router.component";
import "./manage/edit-data-owner.component";
import "./manage/delete-team-confirmation.component";
import "./manage/service-tree/edit-team.component";
import "./manage/service-tree/link-team.component";
import "./asset-transfer/asset-transfer-requests.component";
import "./asset-transfer/approve-asset-transfer-request-confirmation.component";
import "./asset-transfer/deny-asset-transfer-request-confirmation.component";

import * as Pdms from "../../../shared/pdms/pdms-types";
import { ConfirmationModalData } from "../../shared/utilities/confirmation-modal-actions.component";
import { OwnerViewModalTriggerData } from "../../shared/data-owner/owner-view-modal-trigger.component";

/** 
 * Common state parameters in edit data owner flows. 
 **/
export interface EditOwnerStateParams extends ng.ui.IStateParamsService {
    /** 
     * Data owner. 
     **/
    dataOwner: Pdms.DataOwner;

    /** 
     * UI signal to indicate the Data Owner has been successfully linked to Service Tree. 
     **/
    isNewlyLinked?: boolean;
}

/** 
 * Expected format for the data passed into Confirmation modals for asset transfer requests 
 **/
export interface AssetTransferRequestsConfirmationModalData extends ConfirmationModalData {
    requests: RequestContainer[];
}

export interface RequestContainer {
    request: Pdms.TransferRequest;
    isChecked: boolean;
    isCollapsed: boolean;
    ownerViewModalTriggerData: OwnerViewModalTriggerData;
}


@Route({
    name: "data-owners",
    options: {
        abstract: true,
        url: "/data-owners",
        template: "<ui-view/>"
    }
})
@Route({    //  Create data owner using a Service Tree service team information.
    name: "data-owners.using-service-tree",
    options: {
        url: "/create/service-tree",
        template: "<pcd-create-service-tree-team></pcd-create-service-tree-team>"
    }
})
@Route({
    name: "data-owners.view",
    options: {
        // NOTE: Please do not change this URL, it is exposed externally.
        url: "/view/{ownerId:guid}",
        template: "<pcd-view-data-owner></pcd-view-data-owner>"
    }
})
@Route({    //  View pending asset transfers.
    name: "data-owners.asset-transfers",
    options: {
        url: "/asset-transfers/{ownerId:guid}",
        template: "<ui-view><pcd-asset-transfer-requests></pcd-pcd-asset-transfer-requests></ui-view>"
    }
})
@Route({
    name: "data-owners.asset-transfers.approve",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-approve-asset-transfer-request-confirmation></pcd-approve-asset-transfer-request-confirmation>"
            }
        }
    }
})
@Route({
    name: "data-owners.asset-transfers.deny",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-deny-asset-transfer-request-confirmation></pcd-deny-asset-transfer-request-confirmation>"
            }
        }
    }
})
@Route({    //  Edit data owner.
    name: "data-owners.edit",
    options: {
        url: "/edit/{ownerId:guid}",
        template: "<ui-view><pcd-edit-data-owner-router></pcd-edit-data-owner-router></ui-view>"
    }
})
@Route({    //  Edit PDMS data owner.
    name: "data-owners.edit.pdms",
    options: {
        params: {
            dataOwner: null
        },
        template: "<pcd-edit-data-owner></pcd-edit-data-owner>"
    }
})
@Route({    //  Edit PDMS data owner to be linked to Service Tree.
    name: "data-owners.edit.pdms.link-service-tree",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-link-service-tree-team></pcd-link-service-tree-team>"
            }
        }
    }
})
@Route({    //  Delete confirmation for a pure PDMS team.
    name: "data-owners.edit.pdms.delete-team",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-delete-team-confirmation></pcd-delete-team-confirmation>"
            }
        }
    }
})
@Route({    //  Edit ST data owner.
    name: "data-owners.edit.service-tree",
    options: {
        params: {
            dataOwner: null,
            isNewlyLinked: false
        },
        template: "<pcd-edit-service-tree-team></pcd-edit-service-tree-team>",        
    }
})
@Route({    //  Edit ST data owner to be linked to Service Tree.
    name: "data-owners.edit.service-tree.link-service-tree",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-link-service-tree-team></pcd-link-service-tree-team>"
            }
        }
    }
})
@Route({    //  Delete confirmation for a PDMS team linked to Service Tree.
    name: "data-owners.edit.service-tree.delete-team",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-delete-team-confirmation></pcd-delete-team-confirmation>"
            }
        }
    }
})
export class __registerUserDataOwnerFlowRoutes { }

export function registerContactDataOwnerRoutes($stateProvider: ng.ui.IStateProvider, parentState: string) {
    $stateProvider.state(`${parentState}.owner-contact`, {
        views: {
            "modalContent@": {
                template: "<pcd-owner-view-modal-trigger></pcd-owner-view-modal-trigger>"
            }
        }
    });
}
