import { Route, Config, Inject } from "../../../module/app.module";
import { registerNgpWarningPromptRoute } from "../../shared/utilities/ngp-warning-prompt.component";

import "./create/create-data-asset.component";
import "./manage/manage-data-assets.component";
import "./manage/select-data-agent.component";
import "./manage/link-data-agent.component";
import "./manage/privacy-actions-details.component";
import "./manage/select-variant.component";
import "./manage/link-variant.component";
import "./manage/select-transfer-owner.component";
import "./manage/transfer.component";
import "./variants/variants.component";
import "./variants/variant-requests.component";
import "./variants/variant-details.component";
import "./variants/variant-request-details.component";
import "./variants/delete-variant-request-confirmation.component";
import "./view/view-data-asset.component";
import "./variants/unlink-variant-confirmation.component";
import "./manage/edit-data-asset.component";

/** 
 * Common state parameters in data asset management flows. 
 **/
export interface StateParams extends ng.ui.IStateParamsService {
    /** 
     * Data owner ID. 
     **/
    ownerId: string;
}

export interface VariantsStateParams extends StateParams {
    assetGroupId: string;
}

export interface VariantRequestStateParams extends StateParams {
    /** 
     * Data asset group ID. 
     **/
    assetGroupId: string;
}

@Route({
    name: "data-assets",
    options: {
        abstract: true,
        url: "/data-assets",
        template: "<ui-view/>"
    }
})
@Route({    //  Create data asset, when owner is known.
    name: "data-assets.create",
    options: {
        url: "/create/{ownerId:guid}",
        template: "<ui-view><pcd-create-data-asset></pcd-create-data-asset></ui-view>"
    }
})
@Route({    //  Edit data asset that belong to an owner in the context of data asset management.
    name: "data-assets.create.edit-asset",
    options: {
        url: "/{assetId:guid}",
        params: {
            returnState: "^"
        },
        template: "<pcd-edit-data-asset></pcd-edit-data-asset>"
    }
})
@Route({
    name: "data-assets.view",
    options: {
        // NOTE: Please do not change this URL, it is exposed externally.
        url: "/view/{assetId:guid}",
        template: "<pcd-view-data-asset></pcd-view-data-asset>"
    }
})

/** 
 * Data Asset management related routes. 
 **/
@Route({    //  Manage data assets that belong to an owner.
    name: "data-assets.manage",
    options: {
        url: "/manage/{ownerId:guid}",
        template: "<ui-view><pcd-manage-data-assets></pcd-manage-data-assets></ui-view>"
    }
})
@Route({    //  Edit data asset.
    name: "data-assets.manage.edit",
    options: {
        url: "/edit/{assetGroupId:guid}",
        template: "<pcd-edit-data-asset></pcd-edit-data-asset>"
    }
})
@Route({    //  Details on the privacy actions for an asset group.
    name: "data-assets.manage.privacy-actions",
    options: {
        url: "/privacy-actions/{assetGroupId:guid}",
        template: "<pcd-privacy-actions-details></pcd-privacy-actions-details>"
    }
})
@Route({    //  View data asset variants.
    name: "data-assets.manage.variants",
    options: {
        url: "/variants/{assetGroupId:guid}",
        template: "<ui-view><pcd-variants></pcd-variants></ui-view>"
    }
})
@Route({    //  Unlink variant
    name: "data-assets.manage.variants.unlink",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-unlink-variant-confirmation></pcd-unlink-variant-confirmation>"
            }
        }
    }
})
@Route({    //  View variant details
    name: "data-assets.manage.variants.details",
    options: {
        url: "/details/{variantId:guid}",
        template: "<pcd-variant-details></pcd-variant-details>"
    }
})
@Route({    //  Manage variant requests for a data asset.
    name: "data-assets.manage.variant-requests",
    options: {
        url: "/variant-requests/{assetGroupId:guid}",
        template: "<ui-view><pcd-variant-requests></pcd-variant-requests></ui-view>"
    }
})
@Route({    //  Delete variant requests
    name: "data-assets.manage.variant-requests.delete",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-delete-variant-request-confirmation></pcd-delete-variant-request-confirmation>"
            }
        }
    }
})
@Route({    //  View details on a variant request.
    name: "data-assets.manage.variant-requests.details",
    options: {
        url: "/details/{variantRequestId:guid}",
        template: "<pcd-variant-request-details></pcd-variant-request-details>"
    }
})
@Route({
    name: "data-assets.manage.variant-requests.details.delete",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-delete-variant-request-confirmation></pcd-delete-variant-request-confirmation>"
            }
        }
    }
})
@Route({
    name: "data-assets.manage.delete-asset-group",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-delete-asset-group-confirmation></pcd-delete-asset-group-confirmation>"
            }
        }
    }
})
@Route({
    name: "data-assets.manage.select-variant",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-select-variant></pcd-select-variant>"
            }
        }
    }
})
@Route({
    name: "data-assets.manage.link-variant",
    options: {
        views: {
            "modalContent@": {
                template: "<pcd-link-variant></pcd-link-variant>"
            }
        }
    }
})
export class __registerUserDataAssetFlowRoutes {
    @Config()
    @Inject("$stateProvider")
    private static registerRoutes($stateProvider: ng.ui.IStateProvider): void {
        registerManageDataAssetsRoutes($stateProvider, "data-assets.manage");
        registerNgpWarningPromptRoute($stateProvider, "data-assets.manage");
        registerTransferDataAssetsRoutes($stateProvider, "data-assets.manage");
    }
}

export function registerManageDataAssetsRoutes($stateProvider: ng.ui.IStateProvider, parentState: string) {
    // select data agent for linking data assets
    $stateProvider.state(`${parentState}.select-agent`, {
        views: {
            "modalContent@": {
                template: "<pcd-select-data-agent></pcd-select-data-agent>"
            }
        }
    });

    // link data agent to data assets
    $stateProvider.state(`${parentState}.link-data-agent`, {
        views: {
            "modalContent@": {
                template: "<pcd-link-data-agent></pcd-link-data-agent>"
            }
        }
    });
}

export function registerTransferDataAssetsRoutes($stateProvider: ng.ui.IStateProvider, parentState: string) {
    // select data owners to transfer data assets
    $stateProvider.state(`${parentState}.select-transfer-owner`, {
        views: {
            "modalContent@": {
                template: "<pcd-select-transfer-owner></pcd-select-transfer-owner>"
            }
        }
    });

    // initiate transfer of data assets
    $stateProvider.state(`${parentState}.transfer`, {
        views: {
            "modalContent@": {
                template: "<pcd-transfer-asset-group></pcd-transfer-asset-group>"
            }
        }
    });
}
