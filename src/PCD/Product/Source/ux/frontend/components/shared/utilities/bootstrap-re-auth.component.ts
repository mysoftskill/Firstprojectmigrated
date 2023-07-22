import { Component, Inject, Route } from "../../../module/app.module";

@Route({
    name: "bootstrap-re-auth",
    options: {
        url: "^/auth",
        template: "<pcd-bootstrap-re-auth></pcd-bootstrap-re-auth>"
    }
})
@Component({
    name: "pcdBootstrapReAuth",
    options: {}
})
@Inject("$window")
export default class BootstrapReAuth implements ng.IComponentController {
    constructor(
        private readonly $window: ng.IWindowService
    ) { }

    // This page is used to invoke the logic of signing in a user via our application bootsrap.
    // It configures the authentication for the session, so the orignal caller should be authenticated once this initialization happens.
    public $onInit(): void {
        // Successfully signed in, so close the temporary authentication page.
        this.$window.close();
    }
}
