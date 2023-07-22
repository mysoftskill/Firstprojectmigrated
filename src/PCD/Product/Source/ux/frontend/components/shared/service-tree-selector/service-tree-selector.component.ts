import { Component, Inject } from "../../../module/app.module";
import template = require("./service-tree-selector.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { IStringFormatFilter } from "../../../shared/filters/string-format.filter";

import { DisplayData } from "../../shared/directory-resource-selector/directory-resource-selector-types";
import { IAngularFailureResponse } from "../../../shared/ajax.service";
import { ErrorCodeHelper } from "../utilities/error-code-helper";
import { Lazy } from "../../../shared/utilities/lazy";

const useCmsHere_SearchNoSuggestionLabel = "No services found.";
const useCmsHere_SearchDisplayLabel = "Search in Service Tree";
const useCmsHere_SearchPlaceholderLabel = "Team name";
const useCmsHere_ServiceNameFormat = "{0} ({1})";
const useCmsHere_ServiceNameFormatWithId = "{0} ({1}) ({2})";
const useCmsHere_ServiceTreeIdKind_Service = "Service";
const useCmsHere_ServiceTreeIdKind_ServiceGroup = "Service Group";
const useCmsHere_ServiceTreeIdKind_TeamGroup = "Team Group";

interface ServiceTreeEntitySelectorItem extends Pdms.STEntityBase {
    name: string;
}

@Component({
    name: "pcdServiceTreeSelector",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$q", "stringFormatFilter", "$meeComponentRegistry")
export default class ServiceTreeSelectorComponent implements ng.IComponentController {
    public searchNoSuggestionLabel = useCmsHere_SearchNoSuggestionLabel;
    public searchDisplayLabel = useCmsHere_SearchDisplayLabel;
    public searchPlaceholderLabel = useCmsHere_SearchPlaceholderLabel;
    public debounceTimeoutMsec = 800;
    public service: Pdms.STServiceDetails;

    private servicesMetadata: ServiceTreeEntitySelectorItem[];
    private foundNoResults = false;

    // Parent controllers.
    public parentCtrl: Lazy<Pdms.ServiceTreeSelectorParent>;

    private readonly serviceTreeIdKindName: {
        [key in Pdms.STEntityKind]: string
    } = {
            "service": useCmsHere_ServiceTreeIdKind_Service,
            "teamGroup": useCmsHere_ServiceTreeIdKind_TeamGroup,
            "serviceGroup": useCmsHere_ServiceTreeIdKind_ServiceGroup
        };

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $q: ng.IQService,
        private readonly formatString: IStringFormatFilter,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService
    ) {
        this.$meeComponentRegistry.register("ServiceTreeSelectorComponent", "ServiceTreeSelectorComponent", this);

        this.parentCtrl = new Lazy<Pdms.ServiceTreeSelectorParent>(() => {
            let response = this.$meeComponentRegistry
                .getInstancesByClass<Pdms.ServiceTreeSelectorParent>("ServiceTreeSelectorParent");

            if (response.length !== 1) {
                throw new Error("Only one parent is supported, please ensure the parent" +
                    "registers with $meeComponentRegistry in the constructor and deregisters in $onDestroy.");
            }
            return response.pop();
        });
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister("ServiceTreeSelectorComponent");
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("getTeam")
    public getTeam(name: string): ng.IPromise<any> {
        if (name) {
            this.setSelectedService(null);

            let firstServiceIdByName = _.findWhere(this.servicesMetadata, { name: name });
            if (firstServiceIdByName && firstServiceIdByName.id) {
                this.searchPlaceholderLabel = name;
                return this.pdmsDataService.getServiceById(firstServiceIdByName.id, firstServiceIdByName.kind)
                    .then((result: Pdms.STServiceDetails) => {
                        this.setSelectedService(result);
                    })
                    .catch((e: IAngularFailureResponse) => {
                        if (ErrorCodeHelper.getErrorCode(e) === "notFound") {
                            // Service Tree is known to keep search records which correspond to invalid entities.
                            // This means that the user will have an option to select a record that does not exist.
                            // Handle this case in the UI with appropriate messaging.
                            return;
                        }
                        throw e;
                    });
            }
        }

        this.setSelectedService(null);
        return this.$q.resolve();
    }

    public showNoResultsForTeam(): ng.IPromise<any> {
        return this.getTeam("");
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("ownerSearch")
    public getSuggestion(nameToken: string): ng.IPromise<DisplayData[]> {
        if (!nameToken) {
            return this.$q.resolve([]);
        }

        this.foundNoResults = false;
        return this.pdmsDataService.getServicesByName(nameToken)
            .then((results: Pdms.STServiceSearchResult[]) => {
                if (results.length === 0) {
                    this.showNoResultsForTeam().then(() => {
                        this.foundNoResults = true;
                        return [];
                    });
                }

                let searchResultNames = _.map(results, r => r.name);
                let duplicateNames = _.uniq(_.reject(searchResultNames, (item, index, array) => {
                    return _.indexOf(array, item, index + 1) === -1;
                }));

                this.servicesMetadata = _.map(results, r => {
                    return {
                        id: r.id,
                        kind: r.kind,
                        name: this.formatServiceName(r, _.contains(duplicateNames, r.name))
                    };
                });

                return _.map(_.uniq(this.servicesMetadata, sm => sm.name), sm => {
                    return {
                        type: "string",
                        value: sm.name
                    };
                });
            }).catch(() => {
                this.foundNoResults = true;
                return [];
            });
    }

    public isAdminOfService(): boolean {
        //  We won't be actually checking whether the user is admin. This functionality is not
        //  available to us anymore after expanding beyond just service records.
        return !!this.service;
    }

    public hasFoundNoResults(): boolean {
        return !this.service && this.foundNoResults;
    }

    // Sets the service and notifies the appropriate parents.
    private setSelectedService(service: Pdms.STServiceDetails): void {
        this.parentCtrl.getInstance().service = service;
        this.service = service;
    }

    private formatServiceName(serviceRecord: Pdms.STServiceSearchResult, appendId?: boolean): string {
        if (appendId) {
            return this.formatString(useCmsHere_ServiceNameFormatWithId,
                [serviceRecord.name, this.serviceTreeIdKindName[serviceRecord.kind], serviceRecord.id]);
        } else {
            return this.formatString(useCmsHere_ServiceNameFormat, [serviceRecord.name, this.serviceTreeIdKindName[serviceRecord.kind]]);
        }
    }
}
