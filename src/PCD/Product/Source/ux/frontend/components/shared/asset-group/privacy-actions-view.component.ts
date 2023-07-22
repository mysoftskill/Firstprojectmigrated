import { Component, Inject } from "../../../module/app.module";
import template = require("./privacy-actions-view.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { DataAssetHelper } from "../../../shared/data-asset-helper";
import { StringUtilities } from "../../../shared/string-utilities";
import { IStringFormatFilter } from "../../../shared/filters/string-format.filter";

const useCmsHere_NoPrivacyActions = "None";
const useCmsHere_PrivacyActionStrings: { [privacyActionId: string]: string } = {
    Delete: "Delete / Account Close",
    Export: "Export"
};

const useCmsHere_pendingActionsString = "(Pending: {0})";

@Component({
    name: "pcdPrivacyActionsView",
    options: {
        template,
        bindings: {
            assetGroup: "<pcdAssetGroup"
        }
    }
})
@Inject("stringFormatFilter")
export class PrivacyActionsView implements ng.IComponentController {
    //  Input: asset group to determine what privacy actions to show.
    public assetGroup: Pdms.AssetGroup;
    public pendingActionsString: string = useCmsHere_pendingActionsString;

    public constructor(
        private readonly formatFilter: IStringFormatFilter) {
    }

    public hasNoActions(): boolean {
        return DataAssetHelper.hasNoPrivacyActions(this.assetGroup);
    }

    public getPrivacyActionsDisplayString(): string {
        let actions: string[] = DataAssetHelper.getPrivacyActionIds(this.assetGroup);
        return StringUtilities.getCommaSeparatedList(actions, useCmsHere_PrivacyActionStrings, useCmsHere_NoPrivacyActions);
    }

    public hasPendingActions(): boolean {
        return DataAssetHelper.hasPendingPrivacyActions(this.assetGroup);
    }

    public hasPrivacyActionsDetails(): boolean {
        return DataAssetHelper.hasPrivacyActionsDetails(this.assetGroup);
    }

    public getPendingPrivacyActionsDisplayString(): string {
        let pendingActions: string[] = DataAssetHelper.getPendingPrivacyActionIds(this.assetGroup);
        return this.formatFilter(useCmsHere_pendingActionsString,
            StringUtilities.getCommaSeparatedList(pendingActions, useCmsHere_PrivacyActionStrings, ""));
    }
}
