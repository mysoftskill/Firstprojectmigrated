import * as angular from "angular";
import { Component, Inject } from "../../../../module/app.module";
import template = require("./view-asset-group-health.html!text");
import { HealthIcon, AssetGroupRegistrationStatus, RegistrationState }
    from "../../../../shared/registration-status/registration-status-types";
import { IStringFormatFilter } from "../../../../shared/filters/string-format.filter";
import { HealthFilterType } from "./view-agents-health.component";
import { IRegistrationStatusDataService } from "../../../../shared/registration-status/registration-status-data.service";
import ViewAgentHealthComponent from "./view-agent-health.component";
import { Lazy } from "../../../../shared/utilities/lazy";

const useCmsHere_AssetGroupLabelText = "Asset group {0}";

@Component({
    name: "pcdViewAssetGroupHealth",
    options: {
        template,
        bindings: {
            assetGroup: "<",
            indexValue: "@",
            displayByStatus: "@",
        }
    }
})
@Inject("stringFormatFilter", "registrationStatusDataService", "$meeMonitoredOperation", "$meeComponentRegistry")
export default class ViewAssetGroupHealthComponent implements ng.IComponentController {
    //  Inputs
    public assetGroup: AssetGroupRegistrationStatus;
    public indexValue: string;
    public displayByStatus: HealthFilterType;
    public parentCtrl: Lazy<ViewAgentHealthComponent>;

    public constructor(
        private stringFormat: IStringFormatFilter,
        private registrationStatusDataService: IRegistrationStatusDataService,
        private monitoredOperation: MeePortal.OneUI.Angular.IMonitoredOperation,
        private $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService
    ) {
        this.parentCtrl = new Lazy<ViewAgentHealthComponent>(() => 
            this.$meeComponentRegistry.getInstanceById<ViewAgentHealthComponent>("ViewAgentHealthComponent"));
    }

    public getIconState(): HealthIcon {
        if (this.assetGroup.isComplete) {
            return HealthIcon.healthy;
        } else {
            return this.isAssetGroupReloadable(this.assetGroup) ? HealthIcon.unknown : HealthIcon.unhealthy;
        }
    }

    public getAssetGroupLabel(): string {
        return this.stringFormat(useCmsHere_AssetGroupLabelText, this.indexValue);
    }

    private isAssetGroupReloadable(assetGroup: AssetGroupRegistrationStatus): boolean {
        // AssetGroupStatus can be ValidButTruncated for two reasons:
        // 1. When total number of assets found in DataGrid exceeds the limit in PDMS,
        //    only limited number of assets will be loaded.
        // 2. PDMS does not load assets/determine status for AssetGroups[n] where n exceed limit set in PDMS,
        //    in this case there will be no assets. These AssetGroups can be loaded individually.
        return assetGroup.assetsStatus === RegistrationState.validButTruncated && _.isEmpty(assetGroup.assets);
    }

    public loadAssetGroup(): void {
        this.monitoredOperation(this.getOperationName(), () => this.loadAssetGroupStatus());
    }

    public shouldShowLoadLink(): boolean {
        return this.displayByStatus === "all" && this.isAssetGroupReloadable(this.assetGroup);
    }

    public getStatus(): RegistrationState {
        return this.isAssetGroupReloadable(this.assetGroup) ? null : this.assetGroup.assetsStatus;
    }

    public getDrawerState(): string {
        return _.size(this.assetGroup.assets) <= 1 ? "expanded" : "collapsed";
    }

    public shouldShowQualifierInline(): boolean {
         return _.size(this.assetGroup.assets) > 1;
   }

    public shouldShowQualifier(): boolean {
         return !this.shouldShowQualifierInline() && (_.isEmpty(this.assetGroup.assets)
             || (this.displayByStatus === "issues" && _.all(this.assetGroup.assets, asset => asset.isComplete)));
    }

    public getOperationName(): string {
        return `fetchAssetGroup${this.assetGroup.id}`;
    }

    private loadAssetGroupStatus(): ng.IPromise<any> {

        return this.registrationStatusDataService.getAssetGroupStatus(this.assetGroup.id)
            .then((assetGroupRegistrationStatus: AssetGroupRegistrationStatus) => {

                // operating on same object otherwise registrationStatus.AssetGroups[n] doesn't get update
                // which view-agent-health -> updateStatus need to use to recalculate status.
                angular.copy(assetGroupRegistrationStatus, this.assetGroup);
                
                this.parentCtrl.getInstance().updateStatus();
            });
    }
}
