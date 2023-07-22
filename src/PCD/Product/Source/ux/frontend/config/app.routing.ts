import * as angular from "angular";
import { Config, Inject, Run } from "../module/app.module";
import { routeParamGuidType } from "../shared/guid";

type UiRouterStateType = MeePortal.OneUI.Angular.ModalUiRouterState;

/**
 * Configures area routing.
 */
export class AppRouting {
    @Config()
    @Inject("$stateProvider", "$urlRouterProvider", "$locationProvider")
    public static configureAreaRouting($states: ng.ui.IStateProvider, $urlRouterProvider: ng.ui.IUrlRouterProvider, $locationProvider: ng.ILocationProvider): void {
        //  https://docs.angularjs.org/guide/$location#html-link-rewriting
        $locationProvider.html5Mode({ enabled: true, rewriteLinks: false });

        //  All invalid routes point to landing page.
        $urlRouterProvider.otherwise("/");
    }

    @Run()
    @Inject("$rootScope", "$window")
    public static configureClientBiTelemetry($rootScope: ng.IRootScopeService, $window: ng.IWindowService): void {
        let lastKnownUrl = "";
        let lastKnownStateParams: any = {};

        $rootScope.$on("$stateChangeStart", (event: ng.IAngularEvent, toState: UiRouterStateType, toParams: any, fromState: UiRouterStateType, fromParams: any) => {
            if (ignoreStateChange(toState)) {
                return;
            }

            $window.BradburyTelemetry.bi.reportSpaPageView({
                path: `/${toState.name.replace(".", "/")}`,
                viewId: toState.name
            });
        });

        $rootScope.$on("$stateChangeSuccess", (event: ng.IAngularEvent, toState: UiRouterStateType, toParams: any, fromState: UiRouterStateType, fromParams: any) => {
            lastKnownUrl = (toState.url || "").toString();
            lastKnownStateParams = toParams;
        });

        /**
         * Determines whether or not a page transition should be ignored for BI purposes.
         * @param toState The state being transitioned to.
         * @param toParams The parameters for the given state.
         */
        function ignoreStateChange(toState: UiRouterStateType): boolean {
            if (toState._isModal || !toState.url) {
                //  Do not count transitions to modal states or states when there's no URL change as page views.
                return true;
            }

            // URL and state parameters are exactly the same.
            if (lastKnownUrl === toState.url) {
                if (angular.equals(toState.params, lastKnownStateParams)) {
                    return true;
                }
            }

            return false;
        }
    }

    @Config()
    @Inject("$urlMatcherFactoryProvider")
    public static addRouteParamTypes($urlMatcherFactoryProvider: ng.ui.IUrlMatcherFactory): void {

        $urlMatcherFactoryProvider.type("guid", routeParamGuidType($urlMatcherFactoryProvider));
    }
}
