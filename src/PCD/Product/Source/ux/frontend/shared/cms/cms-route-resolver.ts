import { CmsDataRegistryInstance } from "./cms-data-registry";
import { ICmsDataService, CmsContentCollection } from "./cms-types";
import { RouteWithCmsConfigDecorator } from "../../module/app.module";

export class CmsRouteResolver {

    private static loadRequiredCmsData(
        cmsDataService: ICmsDataService, 
        route: RouteWithCmsConfigDecorator,
        $q: ng.IQService): ng.IPromise<CmsContentCollection> {

        // read the comment inside applyCmsRouteResolver on why try/catch is needed
        try {
            let cmsAreasToLoad = _.unique(
                _.compact(
                    _.flatten([route.cms.requiredCmsAreas])
                )
            );
    
            let cmsKeys = CmsDataRegistryInstance.getCmsByAreas(cmsAreasToLoad);
    
            return cmsDataService.loadContentItems(cmsKeys);    

        } catch(exception) {
            return $q.reject(exception);
        }
    }

    public static applyCmsRouteResolver(route: RouteWithCmsConfigDecorator): void {
        if(route.cms && route.cms.requiredCmsAreas) {
                  
            route.options.resolve = _.extend({}, route.options.resolve, {
                pageCms: ["cmsDataService", "$q", (cmsDataService: ICmsDataService, $q: ng.IQService) => {

                    // When CMS content failed to load or TS error inside resolve block, exception is not surfaced to browser
                    // Route would not load and there will be no indication what failed, console.error will inform the dev what failed
                    return CmsRouteResolver.loadRequiredCmsData(cmsDataService, route, $q)
                        .catch((ex) => {
                            console.error("Failed to load CMS content", ex);
                            return $q.reject(ex);
                        });
                }]
            }); 
        }
    }
}


