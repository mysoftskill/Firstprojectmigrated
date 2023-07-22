import * as angular from "angular";
import { Config, Run, Inject } from "../module/app.module";

import { IContactService } from "../shared/contact.service";
import { IGroundControlDataService } from "../shared/flighting/ground-control-data.service";
import { AppConfig } from "../module/data.module";

/**
 * Misc configuration for application.
 * Anything that doesn't warrant its own configuration, goes here.
 */
export class AppMiscConfiguration {
    @Config()
    @Inject("$qProvider")
    public static configureAngularBehaviors($qProvider: ng.IQProvider): void {
        //  Disable $q complaining on unhandled rejected promises.
        $qProvider.errorOnUnhandledRejections(false);
    }

    @Config()
    @Inject("$oneUiDefaults")
    public static configureOneUiDefaults($oneUiDefaults: MeePortal.OneUI.Angular.OneUiDefaults): void {
        $oneUiDefaults.paragraphStyle = "para4";
    }

    @Run()
    @Inject("appConfig", "groundControlDataService")
    public static initializeFlighting(appConfig: AppConfig, groundControlDataService: IGroundControlDataService): void {
        if (!_.contains(appConfig.behaviors, "disable-automatic-flight-discovery")) {
            groundControlDataService.initializeForCurrentUser();
        } else {
            console.debug("Application behavior was modified to prevent automatic ground control service initialization.");
        }
    }

    @Run()
    @Inject("$rootScope", "$window")
    public static configureUiRouterAutoScroll($rootScope: ng.IRootScopeService, $window: ng.IWindowService): void {
        $rootScope.$on("$stateChangeSuccess", (event: ng.IAngularEvent, toState: ng.ui.IState) => $window.scrollTo(0, 0));
    }

    @Run()
    @Inject("$location", "$window")
    public static setupCmsXray($location: ng.ILocationService, $window: ng.IWindowService): void {
        let useCmsXray = "true" === $location.search()["cms-xray"];
        if (useCmsXray) {
            $window.document.body.classList.add("cms-xray");
        } else {
            $window.document.body.classList.remove("cms-xray");
        }
    }

    @Run()
    @Inject("contactService")
    public static setupFeedbackLink(contactService: IContactService): void {
        (<any>window).pcdCollectFeedback = (): void => {
            contactService.collectUserFeedback();
        };

        angular.element("#feedback-footer-link").attr("href", "javascript:pcdCollectFeedback()");
    }
}
