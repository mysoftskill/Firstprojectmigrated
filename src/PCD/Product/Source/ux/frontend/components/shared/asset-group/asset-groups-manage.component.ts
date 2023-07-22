import { Component, Inject } from "../../../module/app.module";
import template = require("./asset-groups-manage.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { ActionVerb } from "../../../shared/pdms-agent-relationship-types";
import { OnAssetGroupAction, AssetGroupsListDisplayOptions } from "./asset-groups-list.component";
import { VariantResource } from "../directory-resource-selector/directory-resource-selector-types";

export interface AssetGroupExtended extends Pdms.AssetGroup {
    checked: boolean;
}

export interface DataAgentLinkingContext {
    assetGroups: Pdms.AssetGroup[];
    agentId?: string;
    agentName?: string;
    verb: ActionVerb;
    onComplete: () => void;
}

export interface TransferAssetGroupContext {
    assetGroups: Pdms.AssetGroup[];
    targetOwnerId?: string;
    targetOwnerName?: string;
    sourceOwnerId: string;
    requestState: Pdms.TransferRequestState;
    onComplete: () => void;
}

export interface VariantLinkingContext {
    assetGroups: Pdms.AssetGroup[];
    variants: VariantResource[];
    ownerId: string;
    onComplete: () => void;
}

@Component({
    name: "pcdAssetGroupsManage",
    options: {
        template,
        bindings: {
            assetGroups: "=pcdAssetGroups",
            refreshList: "&pcdRefreshList",
            onDelete: "&?pcdOnDelete",
            allowVariantManagement: "<?pcdAllowVariantManagement",
            dataAgent: "<?pcdDataAgent"
        },
        transclude: {
            additionalControls: "?pcdAdditionalControls"
        }
    }
})
@Inject("$meeModal", "$stateParams")
export class AssetGroupsManageComponent implements ng.IComponentController {
    //  Input: asset groups to be displayed.
    public assetGroups: AssetGroupExtended[];
    //  Input: function which provides a way to refresh list 
    public refreshList: () => void;
    //  Input: a callback to show a modal delete confirmation and delete the asset group.
    public onDelete: OnAssetGroupAction;
    //  Input: indicates whether or not "Link to a Variant" to be shown.
    public allowVariantManagement: boolean;
    //  Input: data agent
    public dataAgent: Pdms.DataAgent;

    public ownerId: string;
    public assetGroupsListDisplayOptions: AssetGroupsListDisplayOptions;

    constructor(
        private readonly $modalState: MeePortal.OneUI.Angular.IModalStateService,
        private readonly $stateParams: ng.ui.IStateParamsService) { }

    public $onInit(): void {
        this.ownerId = this.$stateParams.ownerId;
        this.assetGroupsListDisplayOptions = {
            allowMultiselect: true,
            showTeamOwned: this.isAgentInContext(),
            showPrivacyActions: true,
            showRemoveAction: !this.isAgentInContext(),
        };

    }

    public anyChecked(): boolean {
        return this.assetGroups.some(ag => ag.checked);
    }

    public linkingEnabled(): boolean {
        return this.assetGroups.some(ag => ag.checked && ag.ownerId === this.ownerId);
    }

    public linkDataAgent(): void {
        // 1. When dataAgent is passed in, this means Manage data assets is performed in the context of a data agent.
        //    User/Team performing this action is the owner of the data agent. Link flow will do re-linking to additional actions,
        //    since agentId is already in context user will be navigated directly to link-data-agent state.
        // 2. No dataAgent is passed in, indicates Manage data assets is performed in the context of a Data Owner. Link flow needs
        //    to capture which data agent to link to, so user will be navigated to select-agent state.
        let firstFlowStepStateId = this.dataAgent ? ".link-data-agent" : ".select-agent";

        this.switchToLinkOrUnlinkFlow(ActionVerb.set, firstFlowStepStateId);
    }

    public unlinkDataAgent(): void {
        this.switchToLinkOrUnlinkFlow(ActionVerb.clear, ".link-data-agent");
    }

    private switchToLinkOrUnlinkFlow(verb: ActionVerb, stateId: string): void {
        let context: DataAgentLinkingContext = {
            assetGroups: this.assetGroups.filter(ag => ag.checked),
            agentId: this.dataAgent ? this.dataAgent.id : null,
            agentName: this.dataAgent ? this.dataAgent.name : null,
            onComplete: () => this.refreshList(),
            verb: verb
        };

        this.$modalState.show("#modal-dialog", stateId, {
            data: context
        });
    }

    public isAgentInContext(): boolean {
        return !!this.$stateParams.agentId;
    }

    public shouldShowLinkingNotAllowedBanner(): boolean {
        return this.assetGroups.some(ag => ag.checked && ag.ownerId !== this.ownerId);
    }

    public onDeleteClicked(assetGroup: Pdms.AssetGroup): ng.IPromise<void> {
        return this.onDelete({ assetGroup });
    }

    public linkVariant(): void {
        let context: VariantLinkingContext = {
            assetGroups: this.assetGroups.filter(ag => ag.checked),
            ownerId: this.ownerId,
            onComplete: () => this.refreshList(),
            variants: []
        };

        this.$modalState.show("#modal-dialog", ".select-variant", {
            data: context
        });
    }

    public transferAssetGroups(): void {
        let context: TransferAssetGroupContext = {
            assetGroups: this.assetGroups.filter(ag => ag.checked),
            sourceOwnerId: this.ownerId,
            requestState: Pdms.TransferRequestState.None,
            onComplete: () => this.refreshList(),
        };

        this.$modalState.show("#modal-dialog", ".select-transfer-owner", {
            data: context
        });
    }
}
