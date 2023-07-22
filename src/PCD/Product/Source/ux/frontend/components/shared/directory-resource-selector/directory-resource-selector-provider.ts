import * as SelectorTypes from "./directory-resource-selector-types";
import * as SharedTypes from "../../../shared/shared-types";
import { appModule } from "../../../module/app.module";

import { SecurityGroupSelector } from "./selectors/security-group-selector";
import { ContactSelector } from "./selectors/contact-selector";
import { NamedResourceSelector } from "./selectors/named-resource-selector";
import { VariantSelector } from "./selectors/variant-selector";
import { SelectorType } from "./directory-resource-selector-types";
import { ApplicationSelector } from "./selectors/application-selector";

/**
 * Interface for AjaxServiceFactory.
 */
export interface IDirectoryResourceSelectorFactory {
    /**
     *  Method to return a AjaxService object. 
     * */
    createInstance: ( type: SelectorType, model: SelectorTypes.DirectoryResourceSelectorExposedData ) => SelectorTypes.IResourceSelector;
}

appModule.factory("directoryResourceSelectorFactory", ["$injector",
    ($injector: ng.auto.IInjectorService): IDirectoryResourceSelectorFactory => {
        return {
            createInstance: (type: SelectorType,
                model: SelectorTypes.DirectoryResourceSelectorExposedData): SelectorTypes.IResourceSelector => {

                    switch (type) {
                        case "security-group":
                            return $injector.instantiate(SecurityGroupSelector, {
                                ngModel: <SelectorTypes.SecurityGroupSelectorData> model,
                            });
                        case "application":
                            return $injector.instantiate(ApplicationSelector, {
                                ngModel: <SelectorTypes.ApplicationSelectorData> model,
                            });
                        case "contact":
                            return $injector.instantiate(ContactSelector, {
                                ngModel: <SelectorTypes.SecurityGroupSelectorData> model,
                            });
                        case "named-resource":
                            return $injector.instantiate(NamedResourceSelector, {
                                ngModel: <SelectorTypes.SecurityGroupSelectorData> model,
                            });
                        case "variant":
                            return $injector.instantiate(VariantSelector, {
                                ngModel: <SelectorTypes.SecurityGroupSelectorData> model,
                            });

                        default:
                            return SharedTypes.invalidConditionBreakBuild(type);
                    }
            }
        };
}]);
