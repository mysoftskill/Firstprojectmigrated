import { Component, Inject} from "../../../../module/app.module";
import template = require("./variant-requests.html!text");
import { BreadcrumbNavigation } from "../../../shared/breadcrumb-heading/breadcrumb-heading.component";
import { VariantRequestStateParams } from "../management-flows";
import { ManageDataAssetsPageBreadcrumb, VariantRequestsPageBreadcrumb } from "../breadcrumbs-config";

import * as Pdms from "../../../../shared/pdms/pdms-types";
import * as Variant from "../../../../shared/variant/variant-types";
import { StringUtilities } from "../../../../shared/string-utilities";
import { IVariantDataService } from "../../../../shared/variant/variant-data.service";

import { DeleteVariantRequestConfirmationModalData } from "./delete-variant-request-confirmation.component";

const useCmsHere_NoTfsUris = "None";

@Component({
    name: "pcdVariantRequests",
    options: {
        template
    }
})
@Inject("$stateParams", "variantDataService", "$meeModal")
export default class VariantRequestsComponent implements ng.IComponentController {
    public breadcrumbs: BreadcrumbNavigation[] = [ManageDataAssetsPageBreadcrumb];
    public pageHeading: string = VariantRequestsPageBreadcrumb.headingText;

    public requests: Variant.VariantRequest[] = [];
    public assetGroupId: string;
    public owner: Pdms.DataOwner;

    constructor(
        private readonly $stateParams: VariantRequestStateParams,
        private readonly variantDataService: IVariantDataService,
        private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): ng.IPromise<any> {
        this.assetGroupId = this.$stateParams.assetGroupId;
        return this.loadVariantRequests();
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("loadVariantRequests")
    public loadVariantRequests(): ng.IPromise<any> {
        return this.variantDataService.getVariantRequestsByOwnerId(this.$stateParams.ownerId, this.assetGroupId)
            .then((variantRequests: Variant.VariantRequest[]) => {
                this.requests = variantRequests;
            });
    }

    public getTfsTrackingUris(uris: string[]): string {
        return StringUtilities.getCommaSeparatedList(uris, {}, useCmsHere_NoTfsUris);
    }

    public showDeleteVariantRequestConfirmationDialog(request: Variant.VariantRequest): void {
        let data: DeleteVariantRequestConfirmationModalData = {
            variantRequest: request,
            onConfirm: () => this.variantDataService.deleteVariantRequestById(request.id).then(() => { this.loadVariantRequests(); }),
            // "^" redirect the modal to this page, "^.^" redirects to the parent (manage data assets) page.
            returnLocation: (this.requests.length > 1) ? "^" : "^.^",
            returnLocationOnCancel: "^"
        };
        this.$meeModal.show("#modal-dialog", ".delete", { data });
    }
}
