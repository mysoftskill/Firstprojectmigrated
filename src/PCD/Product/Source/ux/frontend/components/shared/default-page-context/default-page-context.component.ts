import { Component, Inject } from "../../../module/app.module";
import template = require("./default-page-context.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { VariantRequest } from "../../../shared/variant/variant-types";
import { IVariantDataService } from "../../../shared/variant/variant-data.service";

const ContextBindingTimeoutMsec = 10 * 1000;

@Component({
    name: "pcdDefaultPageContext",
    options: {
        bindings: {
            ownerId: "<?pcdOwnerId",
            agentId: "<?pcdAgentId",
            assetGroupId: "<?pcdAssetGroupId",
            variantRequestId: "<?pcdVariantRequestId",

            owner: "<?pcdOwner",
            agent: "<?pcdAgent",
            assetGroup: "<?pcdAssetGroup",
            variantRequest: "<?pcdVariantRequest",
        },
        template
    }
})
@Inject("pdmsDataService", "variantDataService", "$timeout", "$q")
export default class DefaultPageContextComponent implements ng.IComponentController {
    //  Inputs: IDs to search for.
    public ownerId: string;
    public agentId: string;
    public assetGroupId: string;
    public variantRequestId: string;

    //  Inputs: Objects in context.
    public owner: Pdms.DataOwner;
    public agent: Pdms.DeleteAgent;
    public assetGroup: Pdms.AssetGroup;
    public variantRequest: VariantRequest;

    private shouldFailWaitingForContextBinding = false;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly variantDataService: IVariantDataService,
        private readonly $timeout: ng.ITimeoutService,
        private readonly $q: ng.IQService) { }

    public $onChanges(changes: ng.IOnChangesObject): ng.IPromise<any> {
        if (_.any(_.keys(changes), changeKey => !!changes[changeKey].currentValue)) {
            return this.getPageContext();
        }

        return this.$q.resolve();
    }

    public getPageContextTrigger(): void {
        this.shouldFailWaitingForContextBinding = false;
        this.getPageContext();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("fetchPageContext")
    public getPageContext(): ng.IPromise<any> {
        // TODO Deliverable 17478811 to move timeout delay to a drawer host component.
        // Use a timeout to wait for the drawers to close (to prevent flicker of an open drawer).
        return this.getPageContextData().then(() => this.$timeout(() => { }));
    }

    public getPageContextData(): ng.IPromise<any> {
        // For PDMS objects which are not already defined, we will query PDMS for the more basic objects
        // to provide context on the page being viewed.

        if (this.shouldShowDataAgent()) {
            return this.ensureDataAgent();
        }

        if (this.shouldShowAssetGroup()) {
            return this.ensureAssetGroup();
        }

        if (this.shouldShowVariantRequest()) {
            return this.ensureVariantRequest();
        }

        if (this.shouldShowDataOwner()) {
            return this.ensureDataOwner();
        }

        return this.shouldFailWaitingForContextBinding ? this.$q.reject() : this.$timeout(() => {
            this.shouldFailWaitingForContextBinding = true;
            return this.getPageContext();
        }, ContextBindingTimeoutMsec);
    }

    private shouldShowDataAgent(): boolean {
        return !!this.agent || !!this.agentId;
    }
    private ensureDataAgent(agentId?: string): ng.IPromise<any> {
        if (this.agent) {
            return this.ensureDataOwner(this.agent.ownerId);
        }

        return this.pdmsDataService.getDeleteAgentById(agentId || this.agentId)
            .then((agent: Pdms.DeleteAgent) => {
                this.agent = agent;
                this.agentId = agent.id;
                return this.ensureDataOwner(agent.ownerId);
            });
    }

    private shouldShowAssetGroup(): boolean {
        return !!this.assetGroup || !!this.assetGroupId;
    }
    private ensureAssetGroup(assetGroupId?: string): ng.IPromise<any> {
        if (this.assetGroup) {
            return this.ensureDataOwner(this.assetGroup.ownerId);
        }

        return this.pdmsDataService.getAssetGroupById(assetGroupId || this.assetGroupId)
            .then((assetGroup: Pdms.AssetGroup) => {
                this.assetGroup = assetGroup;
                this.assetGroupId = assetGroup.id;
                return this.ensureDataOwner(assetGroup.ownerId);
            });
    }

    private shouldShowVariantRequest(): boolean {
        return !!this.variantRequest || !!this.variantRequestId;
    }
    private ensureVariantRequest(variantRequestId?: string): ng.IPromise<any> {
        if (this.variantRequest) {
            return this.ensureDataOwner(this.variantRequest.ownerId);
        }

        return this.variantDataService.getVariantRequestById(variantRequestId || this.variantRequestId)
            .then((variantRequest: VariantRequest) => {
                this.variantRequest = variantRequest;
                this.variantRequestId = variantRequest.id;
                return this.ensureDataOwner(variantRequest.ownerId);
            });
    }

    private shouldShowDataOwner(): boolean {
        return !!this.owner || !!this.ownerId ||
            this.shouldShowVariantRequest() ||
            this.shouldShowAssetGroup() ||
            this.shouldShowDataAgent();
    }
    private ensureDataOwner(ownerId?: string): ng.IPromise<any> {
        if (this.owner) {
            return this.$q.resolve();
        }

        return this.pdmsDataService.getDataOwnerWithServiceTree(ownerId || this.ownerId)
            .then((owner: Pdms.DataOwner) => {
                this.owner = owner;
                this.ownerId = owner.id;
            });
    }
}
