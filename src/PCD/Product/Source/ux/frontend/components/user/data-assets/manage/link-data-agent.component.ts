import { Component, Inject } from "../../../../module/app.module";
import template = require("./link-data-agent.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";
import { Capability, ActionVerb, Relationship } from "../../../../shared/pdms-agent-relationship-types";
import { DataAgentLinkingContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { IPcdErrorService } from "../../../../shared/pcd-error.service";
import { IAngularFailureResponse } from "../../../../shared/ajax.service";

export interface LinkUnlinkStrings {
    buttonLabel: string;
    title: string;
    actionsLabel: string;
    selectedDataAgentLabel: string;
}

const errorCategory = "manage-data-assets.link-agent";

const useCmsHere_LinkButtonLabel = "Link";
const useCmsHere_LinkTitle = "Link to data agent";
const useCmsHere_LinkActionsLabel = "Select the privacy actions to link to this data agent";
const useCmsHere_LinkSelectedDataAgentLabel = "Linking to data agent";

const useCmsHere_UnlinkButtonLabel = "Unlink";
const useCmsHere_UnlinkTitle = "Unlink from data agent";
const useCmsHere_UnlinkActionsLabel = "Select the privacy actions to unlink from this data agent";
const useCmsHere_UnlinkNoAgentActionsLabel = "Select the privacy actions to unlink";
const useCmsHere_UnlinkSelectedDataAgentLabel = "Unlinking from data agent";

@Component({
    name: "pcdLinkDataAgent",
    options: {
        template,
        bindings: {
        }
    }
})
@Inject("pdmsDataService", "$meeModal", "pcdErrorService")
export default class LinkDataAgentComponent implements ng.IComponentController {
    public deleteSelected = false;
    public exportSelected = false;
    public hasAcknowledgedUnlink = false;
    public context: DataAgentLinkingContext;
    public dataAgentName: string;
    public errorCategory = errorCategory;
    public isAgentNameRequired = true;
    public strings: LinkUnlinkStrings;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $modalState: MeePortal.OneUI.Angular.IModalStateService,
        private readonly pcdError: IPcdErrorService
    ) { }

    public $onInit() {
        this.context = this.$modalState.getData<DataAgentLinkingContext>();

        // When unlinking in the context of Data Owner, there is no agentId in the context so agent Name should not be displayed
        // in all other cases this will be true
        this.isAgentNameRequired = !!this.context.agentId;

        this.dataAgentName = this.context.agentName;

        this.setStrings();
        this.pcdError.resetErrorsForCategory(this.errorCategory);
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    public linkOrUnlink(): ng.IPromise<any> {
        this.pcdError.resetErrorForId(`${this.errorCategory}.link-or-unlink`);

        return this.performLinkOrUnlink(this.context.agentId, this.context.verb)
            .then(() => {
                this.context.onComplete();
                if (this.context.verb === ActionVerb.set) {
                    this.$modalState.switchTo("^.ngp-warning-prompt");
                } else {
                    this.$modalState.hide("^");
                }
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, { genericErrorId: "link-or-unlink" });
            });
    }

    public isUnlinkFlow(): boolean {
        return ActionVerb.clear === this.context.verb;
    }

    public cancel(): void {
        this.$modalState.hide("^");
    }

    public disallowedToLinkOrUnlink(): boolean {
        let allowedToLinkOrUnlink = this.deleteSelected || this.exportSelected;
        if (this.isUnlinkFlow()) {
            allowedToLinkOrUnlink = allowedToLinkOrUnlink && this.hasAcknowledgedUnlink;
        }

        return !allowedToLinkOrUnlink;
    }

    private setStrings(): void {
        if (this.context.verb === ActionVerb.set) {
            this.strings = {
                buttonLabel: useCmsHere_LinkButtonLabel,
                actionsLabel: useCmsHere_LinkActionsLabel,
                selectedDataAgentLabel: useCmsHere_LinkSelectedDataAgentLabel,
                title: useCmsHere_LinkTitle,
            };
        } else {
            this.strings = {
                buttonLabel: useCmsHere_UnlinkButtonLabel,
                actionsLabel: this.isAgentNameRequired ? useCmsHere_UnlinkActionsLabel : useCmsHere_UnlinkNoAgentActionsLabel,
                selectedDataAgentLabel: useCmsHere_UnlinkSelectedDataAgentLabel,
                title: useCmsHere_UnlinkTitle,
            };
        }
    }

    private performLinkOrUnlink(agentId: string, verb: ActionVerb): ng.IPromise<void> {
        return this.pdmsDataService.setAgentRelationshipsAsync({
            relationships: this.context.assetGroups.map<Relationship>(ag => {
                let applicableCapabilities = this.getApplicableCapabilities(ag, agentId, this.context.verb);

                return {
                    assetGroupId: ag.id,
                    assetGroupETag: ag.eTag,
                    actions: applicableCapabilities.map(capability => {
                        return {
                            capability: capability,
                            verb: verb,
                            agentId: (verb === ActionVerb.set ? this.context.agentId : null)
                        };
                    })
                };
            })
        });
    }

    private getApplicableCapabilities(assetGroup: Pdms.AssetGroup, agentId: string, verb: ActionVerb): Capability[] {

        // when user performs unlink in the context of a agentId, they are only unlinking capabilities associated to the agentId in context
        // i.e.  agent id "1" in context, below is state of selected assetGroups
        // assetGroupId     deleteAgentId     exportAgentId
        // "91"              "1"              "2"
        // "92"              "3"              "1"
        // for unlinking below payload need to be sent:
        // assetGroupId  capability
        // "91"          delete
        // "92"          export
        if (verb === ActionVerb.clear && agentId) {
            return this.getCapabilitiesLinkedToAgent(assetGroup, agentId);
        } else {
            // 1. when unlinking, agentId not in context, all capabilities can be unlinked irrespective of the agentId
            // 2. when linking, agentId is in context, linking current agentId on all selected assetGroups/capabilities is a valid scenario.
            // 3. when linking, agentId is not in context, linking flow will force you to select a agentId after that #2 applies.
            return this.getSelectedCapabilities();
        }
    }

    private getSelectedCapabilities(): Capability[] {
        let capabilities: Capability[] = [];

        if (this.exportSelected) {
            capabilities.push(Capability.export);
        }

        if (this.deleteSelected) {
            capabilities.push(Capability.delete);
        }

        return capabilities;
    }

    private getCapabilitiesLinkedToAgent(assetGroup: Pdms.AssetGroup, agentId: string): Capability[] {
        let capabilities: Capability[] = [];
        if (assetGroup.deleteAgentId === agentId && this.deleteSelected) {
            capabilities.push(Capability.delete);
        }

        if (assetGroup.exportAgentId === agentId && this.exportSelected) {
            capabilities.push(Capability.export);
        }
        return capabilities;
    }
}
