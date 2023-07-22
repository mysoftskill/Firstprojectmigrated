import { Component, Inject } from "../../../module/app.module";
import template = require("./manage-variant-requests.html!text");

import * as SelectList from "../../../shared/select-list";
import { IVariantAdminDataService } from "../../../shared/variant-admin/variant-admin-data.service";
import { VariantRequest, AssetGroupVariant } from "../../../shared/variant/variant-types";
import { IStringFormatFilter } from "../../../shared/filters/string-format.filter";
import { ManageVariantRequestsPageBreadcrumb } from "../route-config";

const useCmsHere_VariantRequestFormat = "{0} (Data assets: {1})";

@Component({
    name: "pcdManageVariantRequests",
    options: {
        template
    }
})
@Inject("stringFormatFilter", "variantAdminDataService")
export default class ManageVariantRequestsComponent implements ng.IComponentController {
    public pageHeading = ManageVariantRequestsPageBreadcrumb.headingText;

    public allVariantRequests: VariantRequest[];
    public dataOwnerPickerModel: SelectList.Model;

    public constructor(
        private readonly formatString: IStringFormatFilter,
        private readonly variantAdminData: IVariantAdminDataService
    ) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeManageVariantRequestsComponent")
    public $onInit(): ng.IPromise<any> {
        return this.variantAdminData.getAllVariantRequests()
            .then((response: VariantRequest[]) => {
                this.allVariantRequests = response;

                this.initializeDataOwnerList();
            });
    }

    private initializeDataOwnerList(): void {
        this.dataOwnerPickerModel = {
            selectedId: "",
            items: []
        };

        if (!this.allVariantRequests || !this.allVariantRequests.length) {
            return;
        }

        let allDataOwnersListItems: SelectList.SelectListItem[] = _.map(this.allVariantRequests, (variantRequest: VariantRequest) => {
            return {
                id: variantRequest.ownerId,
                label: variantRequest.ownerName
            };
        });

        this.dataOwnerPickerModel.items = _.uniq(allDataOwnersListItems, (listItem: SelectList.SelectListItem) => listItem.id);
        this.dataOwnerPickerModel.selectedId = this.dataOwnerPickerModel.items[0].id;
        SelectList.enforceModelConstraints(this.dataOwnerPickerModel);
    }

    public getSelectedTeamRequests(ownerId: string): VariantRequest[] {
        return _.filter(this.allVariantRequests, (r: VariantRequest) => r.ownerId === ownerId);
    }

    public getVariantRequestTitle(request: VariantRequest): string {
        let variantNames = _.map(request.requestedVariants, (requestedVariant: AssetGroupVariant) => requestedVariant.variantName)
            .join(", ");
        let numberOfAssetGroupsForRequest = request.variantRelationships.length;

        return this.formatString(useCmsHere_VariantRequestFormat, [variantNames, numberOfAssetGroupsForRequest]);
    }
}
