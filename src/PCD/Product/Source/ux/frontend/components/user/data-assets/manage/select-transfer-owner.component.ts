import { Component, Inject } from "../../../../module/app.module";
import template = require("./select-transfer-owner.html!text");

import * as Pdms from "../../../../shared/pdms/pdms-types";

import { IAngularFailureResponse } from "../../../../shared/ajax.service";
import { TransferAssetGroupContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { DisplayData } from "../../../shared/directory-resource-selector/directory-resource-selector-types";
import { IStringFormatFilter } from "../../../../shared/filters/string-format.filter";

interface DataOwnerEntitySelectorItem {
    id: string;
    name: string;
}

const useCmsHere_OwnerNameFormatWithId = "{0} ({1})";
const useCmsHere_SearchNoSuggestionLabel = "No teams found.";
const useCmsHere_SearchDisplayLabel = "Select a team";
const useCmsHere_SearchPlaceholderLabel = "Team name";

@Component({
    name: "pcdSelectTransferOwner",
    options: {
        template,
        bindings: {
        }
    }
})
@Inject("pdmsDataService", "stringFormatFilter", "$q", "$meeModal")
export default class SelectTransferOwner implements ng.IComponentController {
    public searchNoSuggestionLabel = useCmsHere_SearchNoSuggestionLabel;
    public searchDisplayLabel = useCmsHere_SearchDisplayLabel;
    public searchPlaceholderLabel = useCmsHere_SearchPlaceholderLabel;
    public searchAriaLabel = useCmsHere_SearchPlaceholderLabel;
    public debounceTimeoutMsec = 800;
    private ownersMetadata: DataOwnerEntitySelectorItem[];

    public owner: Pdms.DataOwner;
    public context: TransferAssetGroupContext;

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly formatString: IStringFormatFilter,
        private readonly $q: ng.IQService,
        private readonly $modalState: MeePortal.OneUI.Angular.IModalStateService
    ) { }

    public $onInit() {
        this.context = this.$modalState.getData<TransferAssetGroupContext>();
    }

    public cancel(): void {
        this.$modalState.hide("^");
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("getOwner")
    public getOwner(name: string): ng.IPromise<any> {
        this.owner = null;

        if (name) {
            let firstOwnerByName = _.findWhere(this.ownersMetadata, { name: name });
            if (firstOwnerByName && firstOwnerByName.id) {
                this.searchPlaceholderLabel = name;
                return this.pdmsDataService.getDataOwnerWithServiceTree(firstOwnerByName.id)
                    .then((owner: Pdms.DataOwner) => {
                        this.owner = owner;
                    });
            }
            
        }
        return this.$q.resolve();
    }

    public showNoResultsForTeam(): ng.IPromise<any> {
        return this.getOwner("");
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("ownerSearch")
    public getSuggestion(nameToken: string): ng.IPromise<DisplayData[]> {
        if (!nameToken || !nameToken.trim()) {
            return this.$q.resolve([]);
        }
        
        return this.pdmsDataService.getDataOwnersBySubstring(nameToken)
            .then((results: Pdms.DataOwner[]) => {
                if (results.length === 0) {
                    this.showNoResultsForTeam().then(() => {
                        return [];
                    });
                }
                
                let searchResultNames = _.map(results, r => r.name);
                let duplicateNames = _.uniq(_.reject(searchResultNames, (item, index, array) => {
                    return _.indexOf(array, item, index + 1) === -1;
                }));

                this.ownersMetadata = _.map(results, r => {
                    return {
                        id: r.id,
                        name: this.formatOwnerName(r, _.contains(duplicateNames, r.name))
                    };
                });

                return _.map(_.uniq(this.ownersMetadata, sm => sm.name), sm => {
                    return {
                        type: "string",
                        value: sm.name
                    };
                });
            }).catch(() => {
                return [];
            });
    }

    public isOwnerSelected(): boolean {
        return !!this.owner;
    }

    public next(): void {
        this.context.targetOwnerId = this.owner.id;
        this.context.targetOwnerName = this.owner.name;

        this.$modalState.switchTo("^.transfer");
    }

    private formatOwnerName(owner: Pdms.DataOwner, appendId?: boolean): string {
        return !appendId ? owner.name : this.formatString(useCmsHere_OwnerNameFormatWithId, [owner.name, owner.id]);
    }
}
