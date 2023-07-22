import { Component, Inject, Route } from "../../../module/app.module";
import template = require("./landing.html!text");

import { StateParams } from "../../../shared/search-types";

@Route({
    name: "search",
    options: {
        url: "/search?q",
        params: {
            q: undefined
        },
        template: "<pcd-search-landing></pcd-search-landing>",
    }
})
@Component({
    name: "pcdSearchLanding",
    options: {
        template
    }
})
@Inject("$state", "$stateParams")
export default class SearchLandingComponent implements ng.IComponentController {
    public searchTerms: string;

    constructor(
        private readonly $state: ng.ui.IStateService,
        readonly $stateParams: StateParams) {

        if ($stateParams.q) {
            this.searchTerms = $stateParams.q;
            $state.go(".results");
        }
    }

    public search(): void {
        this.$state.go("search", { q: this.searchTerms });
    }
}
