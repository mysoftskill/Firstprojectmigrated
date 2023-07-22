import { Component, Service, Inject, Route, Config } from "../../../module/app.module";

import * as Pdms from "../../../shared/pdms/pdms-types";
import { registerManageDataAssetsRoutes } from "../data-assets/management-flows";
import { registerContactDataOwnerRoutes } from "../data-owners/management-flows";
import { registerNgpWarningPromptRoute } from "../../shared/utilities/ngp-warning-prompt.component";
import { registerIcmConfirmationModalRoutes } from "../../shared/icm-incident/route-config";
import { ConfirmationModalData } from "../../shared/utilities/confirmation-modal-actions.component";

import "./create/create-data-agent.component";
import "./health/data-agent-health-icon.component";
import "./health/view-agents-health.component";
import "./health/view-agent-health.component";
import "./health/view-asset-groups-health.component";
import "./health/view-asset-group-health.component";
import "./health/view-asset-health.component";
import "./health/view-status-line.component";
import "./manage/edit-data-agent.component";
import "./manage/edit-privacy-actions.component";
import "./manage/manage-data-assets.component";
import "./manage/manage-data-agent-health.component";
import "./manage/manage-data-agents.component";
import "./sharing-requests/pending-sharing-requests.component";
import "./sharing-requests/approve-sharing-requests-confirmation.component";
import "./sharing-requests/deny-sharing-requests-confirmation.component";
import "./operational-readiness/agent-operational-readiness.component";
import "../data-assets/manage/privacy-actions-details.component";
import "./view/view-data-agent-health.component";
import "./view/view-data-agent.component";

/** 
 * Common state parameters in data agent management flows. 
 **/
export interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Data owner ID. 
     **/
    ownerId: string;
}

/** 
 * Expected format for the data passed into Confirmation modals for sharing requests 
 **/
export interface SharingRequestsConfirmationModalData extends ConfirmationModalData {
    requests: RequestContainer[];
}

export interface RequestContainer {
    request: Pdms.SharingRequest;
    isChecked: boolean;
}

@Route({
    name: "data-agents",
    options: {
        abstract: true,
        url: "/data-agents",
        template: "<ui-view/>"
    }
})
@Route({    //  Create data agent, when owner is known.
    name: "data-agents.create",
    options: {
        url: "/create/{ownerId:guid}",
        template: "<pcd-create-data-agent kind='delete-agent'></pcd-create-data-agent>"
    }
})
@Route({
    name: "data-agents.create.share-data-agent",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-sharing-request-contacts-required></pcd-sharing-request-contacts-required>"
            }
        }
    }
})
@Route({
    name: "data-agents.view",
    options: {
        // NOTE: Please do not change this URL, it is exposed externally.
        url: "/view/{agentId:guid}",
        template: "<pcd-view-data-agent></pcd-view-data-agent>"
    }
})
@Route({
    name: "data-agents.health",
    options: {
        // NOTE: Please do not change this URL, it is exposed externally.
        url: "/health/{agentId:guid}",
        template: "<pcd-view-data-agent-health></pcd-view-data-agent-health>"
    }
})
@Route({    // Operational readiness criteria.
    name: "data-agents.operational-readiness",
    options: {
        url: "/operational-readiness/{ownerId:guid}/{agentId:guid}",
        template: "<pcd-agent-operational-readiness></pcd-agent-operational-readiness>"
    }
})

/** 
 * Data Agents management related routes. 
 **/
@Route({    //  Manage data agents that belong to an owner.
    // TODO: Remove workaround when reading data agents is supported. (ownerId)
    name: "data-agents.manage",
    options: {
        url: "/manage/{ownerId:guid}",
        template: "<ui-view><pcd-manage-data-agents></pcd-manage-data-agents></ui-view>"
    }
})
@Route({    //  Delete data agent
    name: "data-agents.manage.delete-data-agent",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-delete-data-agent-confirmation></pcd-delete-data-agent-confirmation>"
            }
        }
    }
})
@Route({    //  Edit data agent.
    name: "data-agents.manage.edit",
    options: {
        url: "/edit/{agentId:guid}",
        template: "<pcd-edit-data-agent></pcd-edit-data-agent>"
    }
})
@Route({
    name: "data-agents.manage.edit.share-data-agent",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-sharing-request-contacts-required></pcd-sharing-request-contacts-required>"
            }
        }
    }
})
@Route({    //  Manage pending sharing requests.
    name: "data-agents.manage.pending-sharing-requests",
    options: {
        url: "/requests/{agentId:guid}",
        template: "<pcd-pending-sharing-requests></pcd-pending-sharing-requests>"
    }
})
@Route({
    name: "data-agents.manage.pending-sharing-requests.approve",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-approve-sharing-request-confirmation></pcd-approve-sharing-request-confirmation>"
            }
        }
    }
})
@Route({
    name: "data-agents.manage.pending-sharing-requests.deny",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-deny-sharing-request-confirmation></pcd-deny-sharing-request-confirmation>"
            }
        }
    }
})
@Route({    //  Manage data assets linked to a data agent.
    name: "data-agents.manage.manage-data-assets",
    options: {
        url: "/data-assets/{agentId:guid}",
        template: "<ui-view><pcd-manage-data-agent-assets></pcd-manage-data-agent-assets></ui-view>"
    }
})
@Route({    //  Details on the privacy actions for an asset group.
    name: "data-agents.manage.manage-data-assets.privacy-actions",
    options: {
        url: "/privacy-actions/{assetGroupId:guid}",
        template: "<pcd-privacy-actions-details></pcd-privacy-actions-details>"
    }
})
@Route({
    name: "data-agents.manage.health",
    options: {
        url: "/health/{agentId:guid}",
        template: "<pcd-manage-data-agent-health></pcd-manage-data-agent-health>"
    }
})

export class __registerUserDataAgentFlowRoutes {
    @Config()
    @Inject("$stateProvider")
    public static registerRoutes($stateProvider: ng.ui.IStateProvider): void {
        registerManageDataAssetsRoutes($stateProvider, "data-agents.manage.manage-data-assets");

        registerNgpWarningPromptRoute($stateProvider, "data-agents.manage.edit");
        registerNgpWarningPromptRoute($stateProvider, "data-agents.create");

        registerContactDataOwnerRoutes($stateProvider, "data-agents.manage.manage-data-assets");
        registerContactDataOwnerRoutes($stateProvider, "data-agents.manage.pending-sharing-requests");
        registerContactDataOwnerRoutes($stateProvider, "data-agents.manage.health");
        registerContactDataOwnerRoutes($stateProvider, "data-agents.health");

        registerIcmConfirmationModalRoutes($stateProvider, "data-agents.view");
    }
}
