import { Component, Inject } from "../../../../module/app.module";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import * as SelectList from "../../../../shared/select-list";
import * as SharedTypes from "../../../../shared/shared-types";
import * as angular from "angular";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";

import template = require("./connection-editor-pivot.html!text");

import AgentConnectionEditorComponent from "./connection-editor.component";
import { DataAgentHelper } from "../../../../shared/data-agent-helper";
import { IContactService } from "../../../../shared/contact.service";
import { IGroundControlApiService } from "../../../../shared/flighting/ground-control-api.service";

const useCmsHere_CosmosPickerLabel = "Cosmos";
const useCmsHere_PCFPickerLabel = "PCF";
const useCmsHere_CommandFeedV2BatchPickerLabel = "CommandFeedV2 (Batch)";
const useCmsHere_CommandFeedV2ContinuousPickerLabel = "CommandFeedV2 (Continuous)";
const useCmsHere_ContactSupportLinkText = "Request assistance";
const useCmsHere_PreProdNonBatchAgentFeatureFlag = "PCD.EUDB.NonBatchAgentPreProd";
const useCmsHere_ProdNonBatchAgentFeatureFlag = "PCD.EUDB.NonBatchAgent";
const useCmsHere_PreProdNonV2ProtocolsFeatureFlag = "PCD.NonV2ProtocolsPreProd";
const useCmsHere_ProdNonV2ProtocolsFeatureFlag = "PCD.NonV2Protocols";
const prodReleaseState = "Prod";
const preProdReleaseState = "PreProd";

type AgentConnectionEditorDictionary = {
    [key: string]: AgentConnectionEditorComponent
};

@Component({
    name: "pcdAgentConnectionEditorPivot",
    options: {
        template,
        bindings: {
            agentId: "<",
            connectionDetailsGroup: "<",
            agentKind: "@",
            hasIcmConnectorId: "<"
        },
    }
})
@Inject("pcdErrorService", "$timeout", "$element", "contactService", "$meeComponentRegistry", "groundControlApiService")
export default class AgentConnectionEditorPivotComponent implements ng.IComponentController {
    //  Input
    public agentId: string;
    public connectionDetailsGroup: Pdms.DataAgentConnectionDetailsGroup;
    public agentKind: Pdms.DataAgentKind = "delete-agent";
    public hasIcmConnectorId = false;

    public displayedReleaseStates: string[] = [];
    public activeReleaseState: string;
    public releaseStatePickerModel: SelectList.Model;

    public readonly errorCategory = `connection-editor-pivot`;
    public protocolPickerModel: SelectList.Model;
    public hasExistingProdConnection = false;
    public hasLegacyConnection = false;
    public allowedReleaseStates: string[] = [];

    public contactSupportLinkText = useCmsHere_ContactSupportLinkText;

    private agentConnectionEditors: AgentConnectionEditorDictionary = {};
    private userFlights: string[];

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly $timeout: ng.ITimeoutService,
        private readonly $element: JQuery,
        private readonly contactService: IContactService,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService,
        private readonly groundControlApiService: IGroundControlApiService
    ) {
        this.$meeComponentRegistry.register("AgentConnectionEditorPivot", "AgentConnectionEditorPivot", this);
    }

    public $onInit(): void {
        this.allowedReleaseStates = Pdms.ReleaseStates.All;
        let defaultReleaseState = this.allowedReleaseStates[0];

        this.pcdError.resetErrorsForCategory(this.errorCategory);
        

        // Initialize protocol picker
        switch (this.agentKind) {
            case "delete-agent":
                let itemsList = [{
                    id: Pdms.PrivacyProtocolId.CommandFeedV2Batch,
                    label: `${useCmsHere_CommandFeedV2BatchPickerLabel}`
                }];

                let CosmosDeleteSignalV2ProtocolItem = {
                    id: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2,
                    label: `${useCmsHere_CosmosPickerLabel} (${Pdms.PrivacyProtocolId.CosmosDeleteSignalV2})`
                };

                let CommandFeedV1ProtocolItem = {
                    id: Pdms.PrivacyProtocolId.CommandFeedV1,
                    label: `${useCmsHere_PCFPickerLabel} (${Pdms.PrivacyProtocolId.CommandFeedV1})`
                };

                let CommandFeedV2ContinuousProtocolItem = {
                    id: Pdms.PrivacyProtocolId.CommandFeedV2Continuous,
                    label: `${useCmsHere_CommandFeedV2ContinuousPickerLabel}`
                };

                if (this.connectionDetailsGroup[Object.keys(this.connectionDetailsGroup)[0]].protocol === Pdms.PrivacyProtocolId.CosmosDeleteSignalV2) {
                    itemsList.push(CosmosDeleteSignalV2ProtocolItem);
                }

                if (this.connectionDetailsGroup[Object.keys(this.connectionDetailsGroup)[0]].protocol === Pdms.PrivacyProtocolId.CommandFeedV1) {
                    itemsList.push(CommandFeedV1ProtocolItem);
                }
                
                if (this.connectionDetailsGroup[Object.keys(this.connectionDetailsGroup)[0]].protocol === Pdms.PrivacyProtocolId.CommandFeedV2Continuous) {
                    itemsList.push(CommandFeedV2ContinuousProtocolItem);
                }

                this.protocolPickerModel = {
                    selectedId: this.connectionDetailsGroup[Object.keys(this.connectionDetailsGroup)[0]].protocol,
                    items: itemsList,
                };
                this.allowedReleaseStates = [
                    Pdms.ReleaseState[Pdms.ReleaseState.PreProd],
                    Pdms.ReleaseState[Pdms.ReleaseState.Prod]
                ];
                break;

            default:
                return SharedTypes.throwUnsupportedLiteralType(this.agentKind);
        }

        // Initialize release state picker
        this.releaseStatePickerModel = {
            selectedId: defaultReleaseState,
            items: [{
                id: defaultReleaseState,
                label: defaultReleaseState
            }],
        };

        // Initialize displayed release states
        _.forEach(this.connectionDetailsGroup, (connectionDetailsGroup) => {
            let connectionState = connectionDetailsGroup.releaseState;

            // If one of the already-configured connectionStates is Prod, connection details are immutable.
            if (connectionState === Pdms.ReleaseState[Pdms.ReleaseState.Prod]) {
                this.hasExistingProdConnection = true;
            }

            // If un-allowed states are pre-configured, we explicitly include them in "allowedReleaseStates"
            if (Pdms.ReleaseStates.Rings.indexOf(connectionState) > -1) {
                this.allowedReleaseStates = _.filter(Pdms.ReleaseStates.All, (releaseState) => {
                    return (this.allowedReleaseStates.indexOf(releaseState) > -1) || (releaseState === connectionState);
                });
            }

            let filteredReleaseStates = _.filter(this.allowedReleaseStates, (releaseState) => {
                return (this.displayedReleaseStates.indexOf(releaseState) > -1) || (releaseState === connectionState);
            });
            this.displayedReleaseStates = filteredReleaseStates.length ? filteredReleaseStates : [defaultReleaseState];
            this.activeReleaseState = this.displayedReleaseStates[0];
        });

        this.getGroundControl().then(() => this.updateReleaseStatePicker());
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister("AgentConnectionEditorPivot");
    }

    public updateAgentConnectionEditors(): void {
        this.agentConnectionEditors = {};
        this.$meeComponentRegistry.getInstancesByClass("AgentConnectionEditorComponent")
            .forEach((connectionEditor: AgentConnectionEditorComponent) => {
                this.agentConnectionEditors[connectionEditor.releaseState] = connectionEditor;
            });
    }

    public resetErrorsOnAllEditors(errorId: string): void {
        this.displayedReleaseStates.forEach(releaseState => {
            this.agentConnectionEditors[releaseState] &&
                this.agentConnectionEditors[releaseState].resetError(errorId);
        });
    }

    public hasErrors(): boolean {
        let errorReleaseState = _.find(this.displayedReleaseStates, (releaseState) => {
            return this.agentConnectionEditors[releaseState].hasErrors();
        });

        if (this.isDuplicateAadAppId() || this.isDuplicateAadAppIds()) {
            this.agentConnectionEditors[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].setDuplicateAadAppIdError();
            this.agentConnectionEditors[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].setDuplicateAadAppIdError();
            return true;
        }
        if (this.isDuplicateMsaSiteId()) {
            this.agentConnectionEditors[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].setDuplicateMsaSiteIdError();
            this.agentConnectionEditors[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].setDuplicateMsaSiteIdError();
            return true;
        }

        if (errorReleaseState) {
            // Open the tab which corresponds to error so that user can instantly fix it.
            this.$element.find(`#tab-title-${errorReleaseState}`).click();
            return true;
        }

        return false;
    }

    public applyChanges(): void {
        this.displayedReleaseStates.forEach(releaseState => {
            this.agentConnectionEditors[releaseState].applyChanges();
        });
    }

    public removeClicked(releaseState: string): void {
        delete this.connectionDetailsGroup[releaseState];

        this.displayedReleaseStates = _.reject(this.displayedReleaseStates, (state) => state === releaseState);
        this.activeReleaseState = this.displayedReleaseStates[0];

        this.updateReleaseStatePicker();
    }

    public addClicked(): void {
        let selectedReleaseState = "";
        if (this.activeReleaseState === prodReleaseState) {
            selectedReleaseState = preProdReleaseState;
        } else {
            selectedReleaseState = prodReleaseState;
        }

        // Add selected connection in group. All properties of first connection are copied over to the newly added connection, except for
        // MSA site ID and AAD app ID which should be different for PreProd and Prod.
        this.connectionDetailsGroup[selectedReleaseState] = angular.copy(this.connectionDetailsGroup[this.displayedReleaseStates[0]]);
        this.connectionDetailsGroup[selectedReleaseState].aadAppId = undefined;
        this.connectionDetailsGroup[selectedReleaseState].msaSiteId = undefined;
        this.connectionDetailsGroup[selectedReleaseState].agentReadiness = Pdms.AgentReadinessState[Pdms.AgentReadinessState.TestInProd];

        let isContinuousAgentEnabledForCurrentEnv = selectedReleaseState === Pdms.ReleaseState[Pdms.ReleaseState.PreProd] ? this.isPreProdNonBatchAgentEnabled() : this.isProdNonBatchAgentEnabled();

        if (!isContinuousAgentEnabledForCurrentEnv && this.connectionDetailsGroup[selectedReleaseState].protocol === Pdms.PrivacyProtocolId.CommandFeedV2Continuous) {
            this.connectionDetailsGroup[selectedReleaseState].protocol = Pdms.PrivacyProtocolId.CommandFeedV1;
        }

        // Add the newly selected release state in right order.
        this.displayedReleaseStates = _.filter(this.allowedReleaseStates, (state) => {
            return (this.displayedReleaseStates.indexOf(state) > -1) || (state === selectedReleaseState);
        });
        this.activeReleaseState = selectedReleaseState;

        this.updateReleaseStatePicker();
    }

    public showAddButton(): boolean {
        return this.displayedReleaseStates.length < this.allowedReleaseStates.length;
    }

    public showRemoveButton(releaseState: string): boolean {
        if (this.hasExistingProdConnection) {
            return this.displayedReleaseStates.length > 1 &&
                releaseState !== Pdms.ReleaseState[Pdms.ReleaseState.Prod];
        }

        return this.displayedReleaseStates.length > 1;
    }

    public showProtocolPicker(): boolean {
        return this.displayedReleaseStates.length === 1 && !this.hasExistingProdConnection;
    }

    public showNewProdConnectionNGPInfraWarning(): boolean {
        return this.showProdImmutableWarning();
    }

    public showProdImmutableWarning(): boolean {
        return !this.hasExistingProdConnection &&
            this.displayedReleaseStates.indexOf(Pdms.ReleaseState[Pdms.ReleaseState.Prod]) > -1;
    }

    public showProdImmutableMessage(): boolean {
        return this.hasExistingProdConnection &&
            this.activeReleaseState === Pdms.ReleaseState[Pdms.ReleaseState.Prod];
    }

    public areConnectionDetailsReadOnly(): boolean {
        return this.hasExistingProdConnection && this.isActiveConnectionDetailProd();
    }

    public isActiveConnectionDetailProd(): boolean {
        return this.activeReleaseState === Pdms.ReleaseState[Pdms.ReleaseState.Prod];
    }

    public setActiveState(releaseState: string): void {
        this.updateProtocol(releaseState);
        this.activeReleaseState = releaseState;
    }

    public requestProdConnectionUpdate(): void {
        this.contactService.requestAdminAssistance("update-prod-connection", {
            entityId: this.agentId
        });
    }

    private getGroundControl(): ng.IPromise<void> {
        return this.groundControlApiService.getUserFlights().then(userFlights => {
            this.userFlights = userFlights.data;
        });
    }
    
    private isPreProdNonV2ProtocolsEnabled(): boolean {
        return this.userFlights.includes(useCmsHere_PreProdNonV2ProtocolsFeatureFlag);
    }

    private isProdNonV2ProtocolsEnabled(): boolean {
        return this.userFlights.includes(useCmsHere_ProdNonV2ProtocolsFeatureFlag);
    }

    private isPreProdNonBatchAgentEnabled(): boolean {
        return this.userFlights.includes(useCmsHere_PreProdNonBatchAgentFeatureFlag);
    }

    private isProdNonBatchAgentEnabled(): boolean {
        return this.userFlights.includes(useCmsHere_ProdNonBatchAgentFeatureFlag);
    }

    private updateProtocol(releaseState: string): void {
        if (this.connectionDetailsGroup[releaseState].protocol === Pdms.PrivacyProtocolId.CommandFeedV1 ||
            this.connectionDetailsGroup[releaseState].protocol === Pdms.PrivacyProtocolId.CosmosDeleteSignalV2 ||
            this.connectionDetailsGroup[releaseState].protocol === Pdms.PrivacyProtocolId.CommandFeedV2Continuous) {
            return;
        }

        let CosmosDeleteSignalV2ProtocolItem = {
            id: Pdms.PrivacyProtocolId.CosmosDeleteSignalV2,
            label: `${useCmsHere_CosmosPickerLabel} (${Pdms.PrivacyProtocolId.CosmosDeleteSignalV2})`
        };

        let CommandFeedV1ProtocolItem = {
            id: Pdms.PrivacyProtocolId.CommandFeedV1,
            label: `${useCmsHere_PCFPickerLabel} (${Pdms.PrivacyProtocolId.CommandFeedV1})`
        };

        let CommandFeedV2ContinuousProtocolItem = {
            id: Pdms.PrivacyProtocolId.CommandFeedV2Continuous,
            label: `${useCmsHere_CommandFeedV2ContinuousPickerLabel}`
        };
        
        if (this.protocolPickerModel.items.filter(x => x.id === CosmosDeleteSignalV2ProtocolItem.id).length > 0) {
            this.protocolPickerModel.items.splice(this.protocolPickerModel.items.indexOf(CosmosDeleteSignalV2ProtocolItem), 1);
        }
        
        if (this.protocolPickerModel.items.filter(x => x.id === CommandFeedV1ProtocolItem.id).length > 0) {
            this.protocolPickerModel.items.splice(this.protocolPickerModel.items.indexOf(CommandFeedV1ProtocolItem), 1);
        }

        if (this.protocolPickerModel.items.filter(x => x.id === CommandFeedV2ContinuousProtocolItem.id).length > 0) {
            this.protocolPickerModel.items.splice(this.protocolPickerModel.items.indexOf(CommandFeedV2ContinuousProtocolItem), 1);
        }

        if (
            (releaseState === Pdms.ReleaseState[Pdms.ReleaseState.PreProd] && this.isPreProdNonBatchAgentEnabled())
            || (releaseState === Pdms.ReleaseState[Pdms.ReleaseState.Prod] && this.isProdNonBatchAgentEnabled())
        ) {
            if (this.protocolPickerModel.items.filter(x => x.id === CommandFeedV2ContinuousProtocolItem.id).length === 0) {
                this.protocolPickerModel.items.push(CommandFeedV2ContinuousProtocolItem);
            }
        }

        if (
            (releaseState === Pdms.ReleaseState[Pdms.ReleaseState.PreProd] && this.isPreProdNonV2ProtocolsEnabled())
            || (releaseState === Pdms.ReleaseState[Pdms.ReleaseState.Prod] && this.isProdNonV2ProtocolsEnabled())
        ) {

            if (this.protocolPickerModel.items.filter(x => x.id === CosmosDeleteSignalV2ProtocolItem.id).length === 0) {
                this.protocolPickerModel.items.push(CosmosDeleteSignalV2ProtocolItem);
            }

            if (this.protocolPickerModel.items.filter(x => x.id === CommandFeedV1ProtocolItem.id).length === 0) {
                this.protocolPickerModel.items.push(CommandFeedV1ProtocolItem);
            }
        }
    }

    private isDuplicateAadAppId(): boolean {
        return this.connectionDetailsGroup &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]] &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].aadAppId &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]] &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].aadAppId &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].aadAppId === this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].aadAppId;

    }

    private isDuplicateAadAppIds(): boolean {
        return this.connectionDetailsGroup &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]] &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].aadAppIds &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]] &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].aadAppIds &&
            (this.containDuplicateEntries(this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].aadAppIds, this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].aadAppIds) ||
            this.containDuplicateEntries(this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].aadAppIds,this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].aadAppIds));

    }

    private containDuplicateEntries(aadAppIds1: string[], aadAppIds2: string[]): boolean {
        for (let id of aadAppIds1) {
            if (aadAppIds2.includes(id)) {
                return true;
            }
        }
        return false;
    }

    private isDuplicateMsaSiteId(): boolean {
        return this.connectionDetailsGroup &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]] &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].msaSiteId &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]] &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].msaSiteId &&
            this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.PreProd]].msaSiteId === this.connectionDetailsGroup[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].msaSiteId;

    }

    private updateReleaseStatePicker(): void {
        let pickerItems = _.reject(this.allowedReleaseStates, (state: string) => {
            return this.displayedReleaseStates.indexOf(state) > -1;
        }).map((state: string) => {
            return {
                id: state,
                label: state,
            };
        });

        this.releaseStatePickerModel = {
            selectedId: pickerItems[0] && pickerItems[0].id,
            items: pickerItems,
        };

        this.updateProtocol(this.activeReleaseState);

        // TODO: Remove this hack when mee-select OneUI component is done properly.
        // Currently the picker shrinks when list changes dynamically.
        // This hack works around this problem by emulating a click on first item of the list.
        this.ghostClickHack();
    }

    private ghostClickHack(): void {
        // The last thing you want is a ghost clicking buttons in your life!
        this.$timeout(() => {
            let pickerEl = angular.element(".release-state-picker").find(".c-menu");
            pickerEl && pickerEl[0]
                && pickerEl[0].children[0]
                && pickerEl[0].children[0].children[0]
                && angular.element(pickerEl[0].children[0].children[0]).click();
        });
    }
}
