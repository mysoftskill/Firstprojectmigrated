import * as angular from "angular";
import { Component, Inject } from "../../../module/app.module";

import template = require("./auth-warning-banner.html!text");

export const AuthWarningBannerShow = "auth-warning-banner-show";
export const AuthWarningBannerHide = "auth-warning-banner-hide";

@Component({
    name: "pcdAuthWarningBanner",
    options: {
        template,
    }
})
@Inject("$scope")
export default class AuthWarningBanner implements ng.IComponentController {
    private isVisible = false;

    constructor(
        private readonly $scope: ng.IScope
    ) { }

    public $onInit(): void {
        this.$scope.$on(AuthWarningBannerShow, (event: ng.IAngularEvent) => {
            this.isVisible = true;
        });

        this.$scope.$on(AuthWarningBannerHide, (event: ng.IAngularEvent) => {
            this.dismissBanner();
        });
    }

    public dismissBanner(): void {
        this.isVisible = false;
    }
}