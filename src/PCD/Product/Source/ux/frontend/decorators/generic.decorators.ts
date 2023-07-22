import * as angular from "angular";

/** 
 * Generic properties of named entity decorator. 
 **/
export interface NamedEntityDecorator {
    /** 
     * Entity name. 
     **/
    name: string;
}

/** 
 * Properties of a route config decorator. 
 **/
export interface RouteConfigDecorator extends NamedEntityDecorator {
    /** 
     * Route options. 
     **/
    options: ng.ui.IState;
}

/** 
 * Properties of a component decorator. 
 **/
export interface ComponentDecorator extends NamedEntityDecorator {
    /** 
     * Component options. 
     **/
    options: ng.IComponentOptions;
}

/**
 * Executes angular.run() for specific module.
 * @param _module Module to run on.
 */
function GenericRun(_module: ng.IModule): any {
    return (target: any, key: string, descriptor: PropertyDescriptor) => {
        _module.run(descriptor.value);
    };
}

/**
 * Executes angular.config() for specific module.
 * @param _module Module to configure.
 */
function GenericConfig(_module: ng.IModule): any {
    return (target: any, key: string, descriptor: PropertyDescriptor) => {
        _module.config(descriptor.value);
    };
}

/**
 * Registers class as a service.
 * @param _module Module to register with.
 * @param service Service options.
 */
function GenericService(_module: ng.IModule, service: NamedEntityDecorator): any {
    return (target: any, key: string, descriptor: PropertyDescriptor) => {
        _module.service(service.name, target);
    };
}

/**
 * Registers function as a filter.
 * @param _module Module to register with.
 * @param filter Filter options.
 */
function GenericFilter(_module: ng.IModule, filter: NamedEntityDecorator): any {
    return (target: any, key: string, descriptor: PropertyDescriptor) => {
        _module.filter(filter.name, descriptor.value);
    };
}

/**
 * Inject dependencies to class or a function.
 * @param dependencies List of dependencies to inject.
 */
function GenericInject(...dependencies: string[]): any {
    return (target: any, key: string, descriptor: PropertyDescriptor) => {
        if (descriptor) {
            const fn = descriptor.value;
            fn.$inject = dependencies;
        } else {
            target.$inject = dependencies;
        }
    };
}

/**
 * Register class as a component.
 * @param _module Module to register with.
 * @param component Component options.
 */
function GenericComponent(_module: ng.IModule, component: ComponentDecorator): any {
    return (target: any, key: string, descriptor: PropertyDescriptor) => {
        _module.component(component.name, angular.merge({
            controller: target,
        }, component.options));
    };
}

/**
 * Configure route.
 * @param _module Module to configure router for.
 * @param route Route options.
 */
function GenericRoute(_module: ng.IModule, route: RouteConfigDecorator): any {
    return (target: any, key: string, descriptor: PropertyDescriptor) => {
        _module.config(["$stateProvider", ($stateProvider: ng.ui.IStateProvider) => {
            $stateProvider.state(route.name, route.options);
        }]);
    };
}

export { GenericConfig, GenericRun, GenericRoute, GenericInject, GenericComponent, GenericService, GenericFilter };
