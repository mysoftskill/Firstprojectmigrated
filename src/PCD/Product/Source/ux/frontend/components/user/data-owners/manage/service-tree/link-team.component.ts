import { Component, Inject } from "../../../../../module/app.module";
import { IAngularFailureResponse } from "../../../../../shared/ajax.service";
import { IPcdErrorService, PcdErrorOverrides } from "../../../../../shared/pcd-error.service";
import * as Pdms from "../../../../../shared/pdms/pdms-types";
import ServiceTreeSelectorComponent from "../../../../shared/service-tree-selector/service-tree-selector.component";

import template = require("./link-team.html!text");
import { Lazy } from "../../../../../shared/utilities/lazy";

const useCmsHere_ServiceDataOwnerInUse = "The Service Tree service you are trying to link is already in use. Please file an issue using 'Issues and feedback'.";

const Link_Service_Tree_To_Owner_Error_Overrides: PcdErrorOverrides = {
    overrides: {
        errorMessages: {
            "alreadyExists": useCmsHere_ServiceDataOwnerInUse
        }
    },
    genericErrorId: "save"
};

@Component({
    name: "pcdLinkServiceTreeTeam",
    options: {
        template
    }
})
@Inject("pcdErrorService", "pdmsDataService", "$meeModal", "$meeComponentRegistry")
export default class LinkServiceTreeTeamComponent implements ng.IComponentController, Pdms.ServiceTreeSelectorParent {
    public errorCategory = "link-service-tree-team";

    public owner: Pdms.DataOwner;
    public service: Pdms.STServiceDetails;
    private serviceTreeSelector: Lazy<ServiceTreeSelectorComponent>;

    constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService
    ) {
        this.$meeComponentRegistry.register("ServiceTreeSelectorParent", "LinkServiceTreeTeamComponent", this);

        this.serviceTreeSelector = new Lazy<ServiceTreeSelectorComponent>(() => 
            this.$meeComponentRegistry.getInstanceById<ServiceTreeSelectorComponent>("ServiceTreeSelectorComponent"));
    }

    public $onInit(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        this.owner = this.$meeModal.getData<Pdms.DataOwner>();
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister("LinkServiceTreeTeamComponent");
    }

    public showAdminInfoBanner(): boolean {
        return this.service && !this.serviceTreeSelector.getInstance().isAdminOfService();
    }

    public canLinkService(): boolean {
        return this.service && this.serviceTreeSelector.getInstance().isAdminOfService();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("modalOperation")
    public linkServiceToTeam(): ng.IPromise<any> {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        let serviceTreeEntity: Pdms.STEntityBase = {
            id: this.service.id,
            kind: this.service.kind
        };

        return this.pdmsDataService.linkDataOwnerToServiceTree(this.owner, serviceTreeEntity)
            .then((updatedOwner: Pdms.DataOwner) => {
                this.$meeModal.hide({
                    stateId: "data-owners.edit.service-tree",
                    stateParams: {
                        dataOwner: updatedOwner,
                        isNewlyLinked: true
                    }
                });
            })
            .catch((e: IAngularFailureResponse) => {
                this.pcdError.setError(e, this.errorCategory, Link_Service_Tree_To_Owner_Error_Overrides);
            });
    }
}
