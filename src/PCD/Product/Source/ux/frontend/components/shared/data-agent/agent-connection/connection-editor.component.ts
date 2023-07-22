import { Component, Inject } from "../../../../module/app.module";
import * as Guid from "../../../../shared/guid";
import { ILockdownService } from "../../../../shared/lockdown.service";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import * as SelectList from "../../../../shared/select-list";
import ConnectionEditorPivotComponent from "./connection-editor-pivot.component";
import template = require("./connection-editor.html!text");
import { Lazy } from "../../../../shared/utilities/lazy";
import { ApplicationSelectorData, NamedResourceSelectorData } from "../../directory-resource-selector/directory-resource-selector-types";
import { ApplicationSelector } from "../../directory-resource-selector/selectors/application-selector";

const useCmsHere_FieldRequired = "This field is required.";
const useCmsHere_UniqueValueRequired = "This field must not be shared between PreProd and Prod.";
const useCmsHere_AppIdNotGuid = "Azure app ID that you have entered doesn't look like a valid GUID. Please check your input.";
const useCmsHere_ProvideIcmConnector = "Please provide IcM connector ID before setting agent to Production Ready.";
const useCmsHere_Lockdown = "NGP Common Infra is in lockdown and not accepting new production agents at this time.";

@Component({
    name: "pcdAgentConnectionEditor",
    options: {
        template,
        bindings: {
            connectionDetails: "<",
            releaseState: "@",
            protocolPickerModel: "<",
            hasIcmConnectorId: "<"
        },
    }
})
@Inject("pcdErrorService", "pdmsDataService", "lockdownService", "$meeComponentRegistry", "$meeUtil")
export default class AgentConnectionEditorComponent implements ng.IComponentController {
    //  Input
    public connectionDetails: Pdms.DataAgentConnectionDetails;
    public releaseState: string;
    public protocolPickerModel: SelectList.Model;
    public hasIcmConnectorId = false;

    public prodReadinessState: string;
    private parentCtrl: Lazy<ConnectionEditorPivotComponent>;
    public errorCategory: string;
    public authTypePickerModel: SelectList.Model;
    public showProdUpgradeWarningText: boolean;
    public hasExistingProdReadyConnection: boolean;
    private connectionEditorId: string;
    public appIdModel: NamedResourceSelectorData = {
        resources: [],
        isAutoSuggestAllowed: false,
        autoSuggestionList: []
    };

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly lockdown: ILockdownService,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService,
        private readonly $meeUtil: MeePortal.OneUI.Angular.IMeeUtil
    ) {
        this.connectionEditorId = $meeUtil.nextUid();
        this.$meeComponentRegistry.register("AgentConnectionEditorComponent", this.connectionEditorId, this);

        this.parentCtrl = new Lazy<ConnectionEditorPivotComponent>(() =>
            this.$meeComponentRegistry.getInstanceById<ConnectionEditorPivotComponent>("AgentConnectionEditorPivot"));
    }

    public $onInit(): void {
        this.errorCategory = `connection-editor-pivot.${this.releaseState.toLowerCase()}`;
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        SelectList.enforceModelConstraints(this.protocolPickerModel);

        
        let msaSiteBasedAuth = {
            id: Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth],
            label: Pdms.AuthenticationTypeName.MsaSiteBasedAuth
        };

        let aadAppBasedAuthItem = {
            id: Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth],
            label: Pdms.AuthenticationTypeName.AadAppBasedAuth
        };

        let itemsList = [aadAppBasedAuthItem];

        // as per the new requirements the "MSA site based auth" option should only be shown for agents which is already using "MSA site based auth"
        if(this.connectionDetails.authenticationType === Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth]) {
            itemsList.unshift(msaSiteBasedAuth);
        }

        this.authTypePickerModel = {
            selectedId: this.connectionDetails.authenticationType,
            items: itemsList
        };
        
        if (this.connectionDetails.aadAppIds) {
            this.appIdModel = {
                resources: this.connectionDetails.aadAppIds.map((appId: string) => {
                    return {
                        id: appId,
                        displayName: appId
                    };
                }),
                isAutoSuggestAllowed: false,
                autoSuggestionList: []
            };
        }

        SelectList.enforceModelConstraints(this.authTypePickerModel);

        this.prodReadinessState = this.connectionDetails.agentReadiness || Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd];
        this.hasExistingProdReadyConnection = this.prodReadinessState === Pdms.AgentReadinessState[Pdms.AgentReadinessState.ProdReady];
        this.showProdUpgradeWarningText = this.hasExistingProdReadyConnection;
        this.protocolPickerModel.selectedId = this.connectionDetails.protocol;
        this.parentCtrl.getInstance().updateAgentConnectionEditors();
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister(this.connectionEditorId);
        this.parentCtrl.getInstance().updateAgentConnectionEditors();
    }

    public resetError(errorId: string): void {
        this.pcdError.resetErrorForId(`${this.errorCategory}.${errorId}`);
    }

    public setDuplicateAadAppIdError(): void {
        this.pcdError.setErrorForId(`${this.errorCategory}.aad-app-id`, useCmsHere_UniqueValueRequired);
    }

    public setDuplicateMsaSiteIdError(): void {
        this.pcdError.setErrorForId(`${this.errorCategory}.msa-site-id`, useCmsHere_UniqueValueRequired);
    }

    public prodReadinessChanged(): void {
        this.showProdUpgradeWarningText = this.prodReadinessState === Pdms.AgentReadinessState[Pdms.AgentReadinessState.ProdReady];

        this.connectionDetails.agentReadiness = this.prodReadinessState;
    }

    public showProdUpgradeWarningInRed(): boolean {
        return !this.hasExistingProdReadyConnection && this.showProdUpgradeWarning();
    }

    public showProdUpgradeWarning(): boolean {
        return this.showProdUpgradeWarningText;
    }

    public disableTestInProdMode(): boolean {
        return this.hasExistingProdReadyConnection;
    }

    public showProdUpgradeDisabledWarning(): boolean {
        return this.disableProdReadyMode();
    }

    public disableProdReadyMode(): boolean {
        return !this.hasIcmConnectorId || (this.lockdown.isActive() && !this.hasExistingProdReadyConnection);
    }

    public getProdUpgradeDisabledWarning(): string {
        if (!this.showProdUpgradeDisabledWarning()) {
            return "";
        }

        if (!this.hasIcmConnectorId) {
            return useCmsHere_ProvideIcmConnector;
        }

        return useCmsHere_Lockdown;
    }

    public hasErrors(): boolean {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        switch (this.connectionDetails.protocol) {
            case Pdms.PrivacyProtocolId.CosmosDeleteSignalV2:
                //  No additional details required.
                break;

            case Pdms.PrivacyProtocolId.CommandFeedV1:
            case Pdms.PrivacyProtocolId.CommandFeedV2Batch:
            case Pdms.PrivacyProtocolId.CommandFeedV2Continuous:
                switch (this.authTypePickerModel.selectedId) {
                    case Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth]:
                        if (!this.connectionDetails.msaSiteId) {
                            this.pcdError.setErrorForId(`${this.errorCategory}.msa-site-id`, useCmsHere_FieldRequired);
                        }
                        break;

                    case Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth]:
                        if (this.connectionDetails.protocol === Pdms.PrivacyProtocolId.CommandFeedV2Batch ||
                            this.connectionDetails.protocol === Pdms.PrivacyProtocolId.CommandFeedV2Continuous) {
                            this.connectionDetails.aadAppIds = [];
                            if (this.appIdModel.resources.length === 0) {
                                this.pcdError.setErrorForId(`${this.errorCategory}.aad-app-id`, useCmsHere_FieldRequired);
                            }
                            this.appIdModel.resources.forEach(element => {
                                if (Guid.isValidGuid(element.id)) {
                                    this.connectionDetails.aadAppIds.push(element.id);
                                } else {
                                    this.pcdError.setErrorForId(`${this.errorCategory}.aad-app-id`, useCmsHere_AppIdNotGuid);
                                }
                            });
                        } else {
                            if (!this.connectionDetails.aadAppId) {
                                this.pcdError.setErrorForId(`${this.errorCategory}.aad-app-id`, useCmsHere_FieldRequired);
                            }
                            if (!Guid.isValidGuid(this.connectionDetails.aadAppId)) {
                                this.pcdError.setErrorForId(`${this.errorCategory}.aad-app-id`, useCmsHere_AppIdNotGuid);
                            }
                        } 
                        break;
                }
                break;

            default:
                throw new Error(`hasErrorsInConnectionDetails: ${this.connectionDetails.protocol} doesn't have input validation rules.`);
        }

        return this.pcdError.hasErrorsInCategory(this.errorCategory);
    }

    public applyChanges(): void {
        this.connectionDetails.protocol = this.protocolPickerModel.selectedId;
        this.connectionDetails.authenticationType = this.authTypePickerModel.selectedId;

        if (Pdms.AuthenticationType.AadAppBasedAuth !== Pdms.AuthenticationType[this.connectionDetails.authenticationType]) {
            delete this.connectionDetails.aadAppId;
        }
        if (Pdms.AuthenticationType.MsaSiteBasedAuth !== Pdms.AuthenticationType[this.connectionDetails.authenticationType]) {
            delete this.connectionDetails.msaSiteId;
        }
    }

    public getSelectedProtocolLabel(): string {
        let selectedId = this.protocolPickerModel.selectedId;
        if(selectedId == null ) {
            selectedId = this.connectionDetails.protocol;
        }
        let selectedProtocolItem = this.protocolPickerModel.items.filter(item => item.id === selectedId)[0];
        return (selectedProtocolItem && selectedProtocolItem.label) || this.protocolPickerModel.selectedId;
    }

    public protocolChanged(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        let protocolId = this.protocolPickerModel.selectedId;
        this.connectionDetails.protocol = protocolId;
        this.pdmsDataService.resetDataAgentConnectionDetails(this.connectionDetails);
        this.protocolPickerModel.selectedId = protocolId;

        this.authTypePickerModel.selectedId = this.connectionDetails.authenticationType;
        SelectList.enforceModelConstraints(this.authTypePickerModel);
    }

    public authTypeChanged(): void {
        this.connectionDetails.aadAppId = null;
        this.aadAppIdChanged();
        this.connectionDetails.msaSiteId = null;
        this.msaSiteIdChanged();

        this.pcdError.resetErrorForId(...["msa-target-site", "msa-site-id", "aad-app-id"].map(field => `${this.errorCategory}.${field}`));
    }

    public aadAppIdChanged(): void {
        this.parentCtrl.getInstance().resetErrorsOnAllEditors("aad-app-id");
    }

    public msaSiteIdChanged(): void {
        this.parentCtrl.getInstance().resetErrorsOnAllEditors("msa-site-id");
    }

    public isActiveConnectionDetailProd(): boolean {
        return this.parentCtrl.getInstance().isActiveConnectionDetailProd();
    }

    public areConnectionDetailsReadOnly(): boolean {
        return this.parentCtrl.getInstance().areConnectionDetailsReadOnly();
    }

    public isMultipleAppIdRequired(): boolean {
        return this.connectionDetails.protocol === Pdms.PrivacyProtocolId.CommandFeedV2Batch ||
            this.connectionDetails.protocol === Pdms.PrivacyProtocolId.CommandFeedV2Continuous;
    }

    public showProtocolPicker(): boolean {
        return this.parentCtrl.getInstance().showProtocolPicker();
    }

    public isAuthTypePickerVisible(): boolean {
        return _.contains(Pdms.ProtocolCapabilityVisibility.AuthTypePicker, this.protocolPickerModel.selectedId);
    }

    public isMsaSiteIdVisible(): boolean {
        if (!_.contains(Pdms.ProtocolCapabilityVisibility.MsaSiteId, this.protocolPickerModel.selectedId)) {
            return false;
        }

        return Pdms.AuthenticationType[Pdms.AuthenticationType.MsaSiteBasedAuth] === this.authTypePickerModel.selectedId;
    }

    public isAadAppIdVisible(): boolean {
        if (!_.contains(Pdms.ProtocolCapabilityVisibility.AadAppId, this.protocolPickerModel.selectedId)) {
            return false;
        }

        return Pdms.AuthenticationType[Pdms.AuthenticationType.AadAppBasedAuth] === this.authTypePickerModel.selectedId;
    }

    public getAuthenticationTypeLabel(): string {
        return Pdms.AuthenticationTypeName[this.connectionDetails.authenticationType];
    }
}
