import { Component } from "../../../../module/app.module";
import template = require("./view-asset-groups-health.html!text");

import { AssetGroupRegistrationStatus } from "../../../../shared/registration-status/registration-status-types";
import { HealthFilterType } from "./view-agents-health.component";

const useCmsHere_NoDataOwner = "No data owner";

@Component({
    name: "pcdViewAssetGroupsHealth",
    options: {
        template,
        bindings: {
            assetGroupsHealth: "<pcdAssetGroups",
            agentOwnerId: "<pcdAgentOwnerId",
            displayByStatus: "@",
        }
    }
})
export default class ViewAssetGroupsHealthComponent implements ng.IComponentController {
    public assetGroupsHealth: AssetGroupRegistrationStatus[];
    public agentOwnerId: string;
    public displayByStatus: HealthFilterType;
    public assetGroupsByOwner: { [ownerName: string]: AssetGroupRegistrationStatus[] };

    public $onInit(): void {
        this.assetGroupsByOwner = _.groupBy(this.assetGroupsHealth, (ag => {
            return ag.ownerName || useCmsHere_NoDataOwner;
        }));
    }

    public shouldShowOwnerContact(assetGroups: AssetGroupRegistrationStatus[]): boolean {
        return this.shouldShowOwnerName(assetGroups) && this.hasOwnerName(assetGroups);
    }

    public shouldShowNoOwner(assetGroups: AssetGroupRegistrationStatus[]): boolean {
        return this.shouldShowOwnerName(assetGroups) && !this.hasOwnerName(assetGroups);
    }

    private hasOwnerName(assetGroups: AssetGroupRegistrationStatus[]): boolean {
        return !!assetGroups.length && !!assetGroups[0].ownerName;
    }

    private shouldShowOwnerName(assetGroups: AssetGroupRegistrationStatus[]): boolean {
        return (this.displayByStatus === "issues" && _.any(assetGroups, ag => !ag.isComplete))
            || this.displayByStatus !== "issues";
    }
}
