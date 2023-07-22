import { Component, Inject } from "../../../module/app.module";
import template = require("./agent-editor.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import * as SelectList from "../../../shared/select-list";
import { IAngularFailureResponse } from "../../../shared/ajax.service";
import { IPcdErrorService, PcdErrorOverrides } from "../../../shared/pcd-error.service";
import * as Guid from "../../../shared/guid";

import AgentConnectionEditorPivotComponent from "./agent-connection/connection-editor-pivot.component";
import { WarningModalData } from "./../utilities/ngp-warning-prompt.component";
import { NamedResourceSelectorData } from "../directory-resource-selector/directory-resource-selector-types";
import { IStringFormatFilter } from "../../../shared/filters/string-format.filter";
import { DataAgentHelper } from "../../../shared/data-agent-helper";
import { Lazy } from "../../../shared/utilities/lazy";
import { IGroundControlApiService } from "../../../shared/flighting/ground-control-api.service";

type OnSavedArgs = {
    updatedAgent: Pdms.DataAgent
};

type IcmConfiguration = "none" | "team-inherited" | "agent-override";

const useCmsHere_FieldRequired = "This field is required.";
const useCmsHere_DataAgentAlreadyExists = "The data agent you're trying to add already exists.";
const useCmsHere_ProdConnectionDetailsNotEditiable = "Connection details cannot be modified when Prod connection is configured.";
const useCmsHere_IcmConnectorNotGuid = "IcM connector ID that you have entered doesn't look like a valid GUID. Please check your input.";
const useCmsHere_IcmConnectorIdDefaultPlaceholder = "e.g., 97CC177C-4531-4D1E-BE76-0E6E058F1CFB";
const useCmsHere_IcmConnectorIdTeamInheritedPlaceholder = "your team's connector ID is currently used to raise incidents";
const useCmsHere_SupportedCloudsPlaceholder = "e.g. {0}";
const useCmsHere_DataResidencyEmptyError = "Data Residency of Deployment Location cannot be empty.";
const useCmsHere_dataResidencyBoundaryGlobalLabel = "Global (Outside EU)";
const useCmsHere_Select = "Select";
const useCmsHere_ResidencyFeatureFlagKey = "PCD.EUDB.Residency";

const doNotLoc_CloudInstanceIds = [
    Pdms.PrivacyCloudInstanceId.Public,
    Pdms.PrivacyCloudInstanceId.Fairfax,
    Pdms.PrivacyCloudInstanceId.Mooncake,
];

const Agent_Editor_Error_Overrides: PcdErrorOverrides = {
    overrides: {
        errorMessages: {
            "alreadyExists": useCmsHere_DataAgentAlreadyExists,
            "immutableValue": useCmsHere_ProdConnectionDetailsNotEditiable
        },
        targetErrorIds: {
            "msaSiteId": "msa-site-id",
            "icm.connectorId": "icm-connector"
        }
    },
    genericErrorId: "save"
};


@Component({
    name: "pcdAgentEditor",
    options: {
        template,
        bindings: {
            owner: "<",
            agent: "<",
            onSaved: "&",
            modalReturnLocation: "<",
        }
    }
})
@Inject("pdmsDataService", "pcdErrorService", "$meeModal", "stringFormatFilter", "$meeComponentRegistry", "groundControlApiService")
export default class EditDataAgentComponent implements ng.IComponentController {
    //  Input
    public owner: Pdms.DataOwner;
    public agent: Pdms.DataAgent;
    public onSaved: (args: OnSavedArgs) => void;
    public modalReturnLocation: string;

    private agentConnectionEditorPivot: Lazy<AgentConnectionEditorPivotComponent>;
    private userFlights: string[];
    public errorCategory = "agent-editor";
    public supportedCloudsPlaceHolder: string;
    private hadProdReadyEnabledForProdState: boolean;
    public supportedCloudsModel: NamedResourceSelectorData = {
        resources: [],
        isAutoSuggestAllowed: true,
        autoSuggestionList: doNotLoc_CloudInstanceIds
    };

    public nonCosmosDeploymentLocationPickerModel: SelectList.Model = {
        selectedId: Pdms.PrivacyCloudInstanceId.Public,
        items: doNotLoc_CloudInstanceIds.map(id => {
            return {
                id: id,
                label: id
            };
        })
    };

    public BoundaryLocationPickerModel: SelectList.Model = {
        selectedId: useCmsHere_Select,
        items: [
            {"id":useCmsHere_Select, "label":useCmsHere_Select },
            {"id":Pdms.BoundaryLocation.EU, "label":Pdms.BoundaryLocation.EU },
            {"id":Pdms.BoundaryLocation.Global, "label":useCmsHere_dataResidencyBoundaryGlobalLabel }
        ]
    };

    public cosmosDeploymentLocationPickerModel: SelectList.Model = {
        selectedId: Pdms.PrivacyCloudInstanceId.Public,
        items: []
    };

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly pcdError: IPcdErrorService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly stringFormatFilter: IStringFormatFilter,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService,
        private readonly groundControlApiService: IGroundControlApiService
    ) {
        this.agentConnectionEditorPivot = new Lazy<AgentConnectionEditorPivotComponent>(() => 
            this.$meeComponentRegistry.getInstanceById<AgentConnectionEditorPivotComponent>("AgentConnectionEditorPivot"));
    }

    public $onChanges(changes: ng.IOnChangesObject): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
        this.supportedCloudsPlaceHolder =
            this.stringFormatFilter(useCmsHere_SupportedCloudsPlaceholder, [Pdms.PrivacyCloudInstanceId.Fairfax]);

        if (this.agent) {
            this.hadProdReadyEnabledForProdState = this.prodReadyEnabledForProdState();

            this.nonCosmosDeploymentLocationPickerModel.selectedId = this.agent.deploymentLocation || Pdms.PrivacyCloudInstanceId.Public;

            this.BoundaryLocationPickerModel.selectedId = this.agent.dataResidencyBoundary;
            if(this.BoundaryLocationPickerModel.selectedId == null) {
                this.BoundaryLocationPickerModel.selectedId = useCmsHere_Select;
            }
            
            this.supportedCloudsModel.resources = this.agent.supportedClouds.map(cloud => {
                return {
                    id: cloud,
                    displayName: cloud
                };
            });

            SelectList.enforceModelConstraints(this.nonCosmosDeploymentLocationPickerModel);
        }
    }

    private prodReadyEnabledForProdState(): boolean {
        return !!this.agent.connectionDetails[Pdms.ReleaseState[Pdms.ReleaseState.Prod]] &&
            this.agent.connectionDetails[Pdms.ReleaseState[Pdms.ReleaseState.Prod]].agentReadiness
            === Pdms.AgentReadinessState[Pdms.AgentReadinessState.ProdReady];
    }

    public saveClicked(): void {
        this.setAgentCloudInstanceValues();

        if (this.hasErrors()) {
            return;
        }

        this.save();
    }

    public verifySharingRequestContacts(): void {
        if (!this.owner.sharingRequestContacts.length) {
            this.$meeModal.show("#modal-dialog", this.modalReturnLocation + ".share-data-agent", {
                data: {
                    owner: this.owner,
                    returnLocation: this.modalReturnLocation
                },
                onDismissed: () => {
                    this.agent.sharingEnabled = false;
                    return {
                        stateId: this.modalReturnLocation
                    };
                }
            });
        }
    }

    public isIcmMandatory() {
        // only when agent is created and team does not have an icm
        return !this.owner.icmConnectorId && !this.agent.name;
    }

    public getIcmConfiguration(): IcmConfiguration {
        if (!this.owner || !this.agent) {
            //  The component is still initializing.
            return "none";
        }

        if (this.owner.icmConnectorId && !this.agent.icmConnectorId) {
            return "team-inherited";
        }

        if (this.agent.icmConnectorId) {
            return "agent-override";
        }

        return "none";
    }

    public disallowConfigureIcmConnectorId(): boolean {
        return "none" === this.getIcmConfiguration();
    }

    public getIcmConnectorIdPlaceholder(): string {
        if ("team-inherited" === this.getIcmConfiguration()) {
            return useCmsHere_IcmConnectorIdTeamInheritedPlaceholder;
        }

        return useCmsHere_IcmConnectorIdDefaultPlaceholder;
    }

    public hideSupportedCloudsInput(): boolean {
        return this.isCosmosAgent() || this.nonCosmosDeploymentLocationPickerModel.selectedId !== Pdms.PrivacyCloudInstanceId.Public;
    }

    public getSupportedCloudsAsLabel(): string {
        if (this.isCosmosAgent()) {
            return Pdms.PrivacyCloudInstanceId.All;
        } else {
            return this.nonCosmosDeploymentLocationPickerModel.selectedId;
        }
    }

    public isCosmosAgent(): boolean {
        let isCosmosAgent = false;

        if (this.agentConnectionEditorPivot.getInstance()) {
            isCosmosAgent = _.any(Object.keys(this.agentConnectionEditorPivot.getInstance().connectionDetailsGroup),
                key => DataAgentHelper.isCosmosProtocol(this.agentConnectionEditorPivot.getInstance().connectionDetailsGroup[key].protocol));
        }

        return isCosmosAgent;
    }

    private isResidencyEnabled(): boolean {
        return this.userFlights && this.userFlights.includes(useCmsHere_ResidencyFeatureFlagKey);
    }

    public onRegionChange(): void {
        this.BoundaryLocationPickerModel.selectedId = useCmsHere_Select;
    }

    public isResidencyRequired(): boolean {
        return !this.isCosmosAgent() && this.isResidencyEnabled() && this.isRegionPublic();
    }

    public isRegionPublic(): boolean {
        return this.nonCosmosDeploymentLocationPickerModel.selectedId === Pdms.PrivacyCloudInstanceId.Public;
    }

    public hasIcmConnectorIdOnTeamOrAgent(): boolean {
        return "none" !== this.getIcmConfiguration();
    }

    private hasErrors(): boolean {
        let hasErrors = false;
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        if (!this.agent.name) {
            this.pcdError.setErrorForId(`${this.errorCategory}.name`, useCmsHere_FieldRequired);
        }

        if (!this.agent.description) {
            this.pcdError.setErrorForId(`${this.errorCategory}.description`, useCmsHere_FieldRequired);
        }

        if (!this.agent.icmConnectorId && this.isIcmMandatory()) {
            this.pcdError.setErrorForId(`${this.errorCategory}.icm-connector`, useCmsHere_FieldRequired);
        }

        if (this.agent.icmConnectorId && !Guid.isValidGuid(this.agent.icmConnectorId)) {
            this.pcdError.setErrorForId(`${this.errorCategory}.icm-connector`, useCmsHere_IcmConnectorNotGuid);
        }

        if (this.agent.supportedClouds.length === 0) {
            this.pcdError.setErrorForId(`${this.errorCategory}.supported-clouds`, useCmsHere_FieldRequired);
        }

        if (this.agent.dataResidencyBoundary === useCmsHere_Select) {
            if(this.isRegionPublic() && this.isResidencyRequired()) {
                this.pcdError.setErrorForId(`${this.errorCategory}.data-boundary`, useCmsHere_DataResidencyEmptyError);
            } else {
                this.agent.dataResidencyBoundary = null;
            }
        }

        if (this.agentConnectionEditorPivot.getInstance().hasErrors()) {
            hasErrors = true;
        }

        hasErrors = hasErrors || this.pcdError.hasErrorsInCategory(this.errorCategory);

        return hasErrors;
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("agentEditorCreate")
    public $onInit(): ng.IPromise<any> {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        return this.groundControlApiService.getUserFlights().then(userFlights => {
            this.userFlights = userFlights.data;
        });
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("agentEditorSave")
    private save(): ng.IPromise<any> {
        this.agentConnectionEditorPivot.getInstance().applyChanges();

        return this.pdmsDataService.updateDataAgent(this.agent)
            .then(agentData => {
                if (!this.hadProdReadyEnabledForProdState && this.prodReadyEnabledForProdState()) {
                    let data: WarningModalData = {
                        onDismiss: () => this.onSaved({ updatedAgent: agentData })
                    };
                    this.$meeModal.show("#modal-dialog", ".ngp-warning-prompt", {
                        data,
                        modalHostOptions: {
                            kind: "close-button"
                        }
                    });
                } else {
                    this.onSaved({ updatedAgent: agentData });
                }
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, Agent_Editor_Error_Overrides);
            });
    }

    private setAgentCloudInstanceValues(): void {
        if (this.isCosmosAgent()) {
            this.agent.deploymentLocation = Pdms.PrivacyCloudInstanceId.Public;
            this.agent.supportedClouds = [Pdms.PrivacyCloudInstanceId.All];
            this.agent.dataResidencyBoundary = Pdms.BoundaryLocation.Global;
        } else {
            this.agent.deploymentLocation = this.nonCosmosDeploymentLocationPickerModel.selectedId;
            this.agent.dataResidencyBoundary = this.BoundaryLocationPickerModel.selectedId;
            
            if (this.agent.deploymentLocation === Pdms.PrivacyCloudInstanceId.Public) {
                this.agent.supportedClouds = this.supportedCloudsModel.resources.map(namedResource => {
                    return namedResource.id;
                });
            } else {
                this.agent.supportedClouds = [this.agent.deploymentLocation];
            }
        }
    }
}
