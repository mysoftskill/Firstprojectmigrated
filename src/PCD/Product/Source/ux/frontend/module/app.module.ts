import * as angular from "angular";

import * as GenericDecorators from "../decorators/generic.decorators";
import { CmsRouteResolver } from "../shared/cms/cms-route-resolver";
import { componentWithCms } from "../shared/cms/component-decorator-helpers";
import { CmsKey, CmsAreaName } from "../shared/cms/cms-types";


let appModule = angular.module("pdmsApp", ["pdmsAppData", "ngSanitize", "ngCookies", "ui.router", "oneui"]);
export { appModule };

let Run: () => any = GenericDecorators.GenericRun.bind(this, appModule);
let Config: () => any = GenericDecorators.GenericConfig.bind(this, appModule);
let Service: (service: GenericDecorators.NamedEntityDecorator) => any = GenericDecorators.GenericService.bind(this, appModule);
let Filter: (filter: GenericDecorators.NamedEntityDecorator) => any = GenericDecorators.GenericFilter.bind(this, appModule);
let Inject = GenericDecorators.GenericInject;
let Component: (component: ComponentWithCmsDecorator) => any = componentDecorator.bind(this, appModule);
let Route: (route: RouteWithCmsConfigDecorator) => any = routeWithCms;

function routeWithCms(route: RouteWithCmsConfigDecorator): any {
    CmsRouteResolver.applyCmsRouteResolver(route);

    return GenericDecorators.GenericRoute(appModule, route);
}

function componentDecorator(_module: ng.IModule, component: ComponentWithCmsDecorator): any {
    if(component.content) {
        return componentWithCms(_module, component);
    } else {
        return GenericDecorators.GenericComponent(_module, component);
    }
}

export { Config, Run, Route, Inject, Component, Service, Filter };

export interface RouteWithCmsConfigDecorator extends GenericDecorators.RouteConfigDecorator {
    cms?: RouteCms;
}

export interface ComponentWithCmsDecorator extends GenericDecorators.ComponentDecorator {
    content?: ComponentCms;
}

export interface ComponentCms extends CmsKey {
    additional?: AdditionalCmsType;
}

export type AdditionalCmsType = {
    [propName: string]: CmsKey
};

export interface RouteCms {
    requiredCmsAreas?: CmsAreaName[];
}
