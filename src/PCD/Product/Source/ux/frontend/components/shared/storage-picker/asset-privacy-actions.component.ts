import { Component, Inject } from "../../../module/app.module";
import template = require("./asset-privacy-actions.html!text");

import { ErrorCategory as StoragePickerErrorCategory } from "./storage-picker.component";
import { DataAgentHelper } from "../../../shared/data-agent-helper";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { DataOwnershipIndicator } from "./asset-owner-indicator.component";

import * as Pdms from "../../../shared/pdms/pdms-types";

type SupportedActionsKey = "deleteAction" | "exportAction";

const useCmsHere_DisabledByDataOwnership = "Your team does not own this data.";
const useCmsHere_DisabledByCannotUnlink = "You cannot remove this action. If you no longer want to cover this asset, you can delete it.";
const useCmsHere_DisabledByExternalAgent = "There is an external agent associated for this action.";
const useCmsHere_DisabledByProtocolSupport = "The data agent protocol does not support this action.";

@Component({
    name: "pcdAssetPrivacyActions",
    options: {
        template,
        bindings: {
            privacyActionState: "=ngModel",
            protocol: "@pcdAgentProtocol",
            isDataOwner: "<pcdIsDataOwner",
            assetGroup: "<?pcdAssetGroup",
            dataAgentId: "<?pcdDataAgentId"
        }
    }
})
@Inject("pcdErrorService")
export class AssetPrivacyActionsComponent implements ng.IComponentController {
    /** 
     * Input: asset supported actions. 
     **/
    public privacyActionState: Pdms.PrivacyActionsState;
    /** 
     * Input: data agent protocol. 
     **/
    public protocol: string;
    /** 
     * Input: is data owner. 
     **/
    public isDataOwner: DataOwnershipIndicator;
    /** 
     * Input: optional asset group to determine external links that exist. 
     **/
    public assetGroup: Pdms.AssetGroup;
    /** 
     * Input: optional data agent context to determine which actions cannot be modified. 
     **/
    public dataAgentId: string;

    //  State to determine whether the input should be enabled.
    public enabledActions: Pdms.PrivacyActionsState;
    /** 
     * Informational text to display to inform why the checkboxes are disabled. 
     **/
    public informationalText: Pdms.PrivacyActionsTips;
    public errorId = `${StoragePickerErrorCategory}.actions`;

    private protocolSupportedPrivacyActions: Pdms.PrivacyActionsState;

    public constructor(
        private readonly pcdError: IPcdErrorService) { }

    public $onInit(): void {
        this.protocolSupportedPrivacyActions = DataAgentHelper.getSupportedAssetPrivacyActions(this.protocol);
        this.resetState();
    }

    public $onChanges(changes: ng.IOnChangesObject): void {
        if (!this.privacyActionState) {
            this.privacyActionState = {
                deleteAction: this.assetGroup && !!this.assetGroup.deleteAgentId,
                exportAction: this.assetGroup && !!this.assetGroup.exportAgentId
            };
        }

        if (changes && (changes["isDataOwner"] || changes["protocol"])) {
            if (changes["protocol"]) {
                this.protocolSupportedPrivacyActions = DataAgentHelper.getSupportedAssetPrivacyActions(this.protocol);
            }

            this.resetState();
        }
    }

    public actionClicked(): void {
        this.clearErrors();
    }

    private clearErrors(): void {
        this.pcdError.resetErrorsForCategory(StoragePickerErrorCategory);
    }

    private resetState(): void {
        this.clearErrors();

        this.informationalText = {
            deleteAction: [],
            exportAction: []
        };

        this.enabledActions = {
            deleteAction: true,
            exportAction: true
        };

        // Invalidate actions by agent.
        if (this.assetGroup && this.dataAgentId) {
            if (this.assetGroup.deleteAgentId && this.assetGroup.deleteAgentId !== this.dataAgentId) {
                this.invalidateAction("deleteAction", useCmsHere_DisabledByExternalAgent);
            }
            if (this.assetGroup.exportAgentId && this.assetGroup.exportAgentId !== this.dataAgentId) {
                this.invalidateAction("exportAction", useCmsHere_DisabledByExternalAgent);
            }
            if (this.isDataOwner === "no") {
                this.invalidateAction("deleteAction", useCmsHere_DisabledByCannotUnlink);
            }
        }

        // Invalidate actions by protocol.
        if (!this.protocolSupportedPrivacyActions["deleteAction"]) {
            this.invalidateAction("deleteAction", useCmsHere_DisabledByProtocolSupport);
        }
        if (!this.protocolSupportedPrivacyActions["exportAction"]) {
            this.invalidateAction("exportAction", useCmsHere_DisabledByProtocolSupport);
        }

        // Invalidate actions by owner.
        if (this.isDataOwner === "no") {
            this.invalidateAction("exportAction", useCmsHere_DisabledByDataOwnership);
        }
    }

    private couldOtherAgentSupportAction(action: SupportedActionsKey): boolean {
        return !!this.assetGroup && !!this.dataAgentId;
    }

    private invalidateAction(action: SupportedActionsKey, message: string): void {
        if (_.contains(Object.keys(this.enabledActions), action)) {
            // Unselect the action only if there's no chance that another agent is already linked to that action.
            if (!this.couldOtherAgentSupportAction(action)) {
                this.privacyActionState[action] = false;
            }

            this.disableAction(action, message);
        }
    }

    private disableAction(action: SupportedActionsKey, message: string): void {
        if (_.contains(Object.keys(this.enabledActions), action)) {
            this.enabledActions[action] = false;
            this.informationalText[action].push(message);
        }
    }
}
