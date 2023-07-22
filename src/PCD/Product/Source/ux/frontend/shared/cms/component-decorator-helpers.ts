import { ICmsDataService, ComponentCmsContentsInternal } from "./cms-types";
import { ComponentWithCmsDecorator } from "../../module/app.module";
import { CmsDataRegistryInstance } from "./cms-data-registry";
import angular = require("angular");

const cmsDataServiceName = "cmsDataService";

/**
 * Register class as a component with Cms contents
 * @param _module Module to register with.
 * @param component Component options.
 */
export function componentWithCms(_module: ng.IModule, component: ComponentWithCmsDecorator): any {
    return function decorator(target: any, key: string, descriptor: PropertyDescriptor) {

        let cmsDataService: ICmsDataService;

        function extendedComponent() {
            cmsDataService = arguments[extendedComponent.$inject.indexOf(cmsDataServiceName)];
            target.apply(this, arguments);
        }

        function getContentItems(): ComponentCmsContentsInternal {

            let additionalContents = {};

            _.map(component.content.additional, (cmsKey, propName) => {
                additionalContents[propName] = cmsDataService.getContentItem(cmsKey);
            });

            return {
                content: cmsDataService.getContentItem(component.content),
                additionalContents: additionalContents
            };
        }

        let contents: ComponentCmsContentsInternal;

        target.prototype._getCmsContents = ():ComponentCmsContentsInternal => {
            return contents || (contents =  getContentItems());
        };

        extendedComponent.prototype = Object.create(target.prototype);
        
        extendedComponent.$inject = _.union(target.$inject, 
            _.indexOf(target.$inject, cmsDataServiceName) === -1 ? [cmsDataServiceName] : []);

        _module.component(component.name, angular.merge({
            controller: extendedComponent,
        }, component.options));

        CmsDataRegistryInstance.registerContent(component.content);
        
        _.forEach(component.content.additional, (cmsKey) => {
            CmsDataRegistryInstance.registerContent(cmsKey);
        });

        return extendedComponent;
    };
}