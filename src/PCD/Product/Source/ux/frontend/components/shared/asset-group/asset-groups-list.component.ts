import { Component, Inject } from "../../../module/app.module";
import template = require("./asset-groups-list.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

import { IServiceTreeDataService } from "../../../shared/service-tree-data.service";

// Display options for a list of assets.
export interface AssetGroupsListDisplayOptions {
    //  Show the column of checkbox per asset group which enables acting on multiple asset groups.
    allowMultiselect?: boolean;

    //  Shows the column that describes who owns the asset group.
    showTeamOwned?: boolean;

    //  Shows the column for the privacy actions of an asset group.
    showPrivacyActions?: boolean;

    //  Shows "Remove" in the action column to delete the asset.
    showRemoveAction?: boolean;
}

export interface AssetGroupExtended extends Pdms.AssetGroup  {
    checked: boolean;
}

export type OnActionArgs = {
    assetGroup: Pdms.AssetGroup
};
export type OnAssetGroupAction = (args: OnActionArgs) => ng.IPromise<void>;
const useCmsHere_IsOwnerLabelYes = "Yes";
const useCmsHere_IsOwnerLabelNo = "No";
const useCmsHere_WhoIsOwner = "(Who?)";

@Component({
    name: "pcdAssetGroupsList",
    options: {
        template,
        bindings: {
            title: "@pcdTitle",
            assetGroups: "=pcdAssetGroups",
            ownerId: "<?pcdOwnerId",
            onDelete: "&?pcdOnDelete",
            displayOptions: "<?pcdDisplayOptions",
        }
    }
})

@Inject("serviceTreeDataService", "$state")
export class AssetGroupsListComponent implements ng.IComponentController {
    //  Input: title of the section/table.
    public title: string;
    //  Input: asset groups to be displayed.
    public assetGroups: AssetGroupExtended[];
    //  Input: owner id in context.
    public ownerId?: string;
    //  Input: a callback to show a modal delete confirmation and delete the asset group.
    public onDelete: OnAssetGroupAction;
    //  Input: display options to determine what the table should look like (default to not showing).
    public displayOptions?: AssetGroupsListDisplayOptions;

    public whoIsOwnerLabel = useCmsHere_WhoIsOwner;

    constructor(
        private readonly serviceTreeDataService: IServiceTreeDataService,
        private readonly $state: ng.ui.IStateService) { }

    public shouldAllowMultiSelect(): boolean {
        return !!this.displayOptions && !!this.displayOptions.allowMultiselect;
    }

    public shouldShowTeamOwned(): boolean {
        return !!this.displayOptions && !!this.displayOptions.showTeamOwned;
    }

    public shouldShowPrivacyActions(): boolean {
        return !!this.displayOptions && !!this.displayOptions.showPrivacyActions;
    }

    public shouldShowRemove(assetGroup: Pdms.AssetGroup): boolean {
        return !!this.displayOptions && !!this.displayOptions.showRemoveAction && !this.hasPendingTransferRequest(assetGroup);
    }

    public hasDataInventory(): boolean {
        return !!this.assetGroups.length;
    }

    public hasVariant(assetGroup: Pdms.AssetGroup): boolean {
        return !!assetGroup.variants && !!assetGroup.variants.length;
    }

    public hasPendingVariantRequests(assetGroup: Pdms.AssetGroup): boolean {
        return !!assetGroup.hasPendingVariantRequests;
    }

    public canSelect(assetGroup: Pdms.AssetGroup): boolean {
        return !this.hasPendingTransferRequest(assetGroup);
    }

    public hasPendingTransferRequest(assetGroup: Pdms.AssetGroup): boolean {
        return !!assetGroup.hasPendingTransferRequest;
    }

    public isOwner(assetGroup: Pdms.AssetGroup): boolean {
        return !!this.ownerId && !!assetGroup.ownerId && assetGroup.ownerId === this.ownerId;
    }

    public shouldShowOwnerContact(assetGroup: Pdms.AssetGroup): boolean {
        return !this.isOwner(assetGroup) && !!assetGroup.ownerId;
    }

    public getIsOwnerLabel(assetGroup: Pdms.AssetGroup): string {
        return this.isOwner(assetGroup) ? useCmsHere_IsOwnerLabelYes : useCmsHere_IsOwnerLabelNo;
    }

    public onDeleteClicked(assetGroup: Pdms.AssetGroup): ng.IPromise<void> {
        return this.onDelete({ assetGroup });
    }

    public getTransferOwnerViewLink(ownerId: string): string {
        return this.$state.href("data-owners.view", { ownerId: ownerId }, { absolute: true });
    }
}
