import { Component, Inject, Route, Config } from "../../../module/app.module";
import template = require("./results.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import * as SearchTypes from "../../../shared/search-types";
import { registerContactDataOwnerRoutes } from "../data-owners/management-flows";
import { IStringFormatFilter } from "../../../shared/filters/string-format.filter";

const useCmsHere_VariantRequestNameFormat = "Variant request by {0}";
const useCmsHere_SharingRequestNameFormat = "Sharing request by {0} for {1} data agent";

@Route({
    name: "search.results",
    options: {
        params: {
            q: ""
        },
        views: {
            "searchResults": {
                template: "<pcd-search-results></pcd-search-results>",
            }
        }
    }
})
@Component({
    name: "pcdSearchResults",
    options: {
        template
    }
})
@Inject("pdmsDataService", "$stateParams", "stringFormatFilter")
export default class SearchResultsComponent implements ng.IComponentController {
    public searchResults: SearchTypes.SearchResults;

    private readonly searchTerms: string;

    @Config()
    @Inject("$stateProvider")
    public static registerRoutes($stateProvider: ng.ui.IStateProvider): void {
        registerContactDataOwnerRoutes($stateProvider, "search.results");
    }

    constructor(
        private readonly pdmsDataService: Pdms.IPdmsDataService,
        private readonly $stateParams: SearchTypes.StateParams,
        private readonly formatFilter: IStringFormatFilter) {

        this.searchTerms = $stateParams.q;
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("searching")
    public $onInit(): ng.IPromise<any> {
        return this.pdmsDataService.search(this.searchTerms)
            .then(results => {
                this.searchResults = results;
            });
    }

    public hasSearchResultsFor(section: SearchTypes.SearchResultsSection): boolean {
        return !!section && !section.isError && !!section.entities && !!section.entities.length;
    }

    public noResults(): boolean {
        if (!this.searchResults) {
            //  We don't know yet: searchResults is never falsy only after we successfully executed request 
            //  to search endpoint.Only then we can inspect it and update UX with appropriate experience.
            return false;
        }

        return _.all(this.getAllSearchSections(), section => !section || !section.entities || !section.entities.length);
    }

    public hasErrors(): boolean {
        if (!this.searchResults) {
            //  We don't know yet: searchResults is never falsy only after we successfully executed request 
            //  to search endpoint.Only then we can inspect it and update UX with appropriate experience.
            //  If request to search endpoint fails completely, progress view will take over the error experience.
            return false;
        }

        return _.some(this.getAllSearchSections(), section => !!section && section.isError);
    }

    public getVariantRequestName(searchResult: SearchTypes.VariantRequestSearchResult): string {
        return this.formatFilter(useCmsHere_VariantRequestNameFormat, searchResult.ownerName);
    }

    public getSharingRequestName(searchResult: SearchTypes.SharingRequestSearchResult): string {
        return this.formatFilter(useCmsHere_SharingRequestNameFormat, [searchResult.ownerName, searchResult.agentId]);
    }

    private getAllSearchSections(): SearchTypes.SearchResultsSection[] {
        return Object.keys(this.searchResults).map(k => this.searchResults[k]);
    }
}
