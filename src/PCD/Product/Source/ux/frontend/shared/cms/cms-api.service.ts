import * as CmsTypes from "./cms-types";
import { Service, Inject } from "../../module/app.module";
import { IAjaxService, IAjaxServiceFactory, IAjaxServiceOptions } from "../ajax.service";
import { IMsalTokenManagerFactory } from "../msal-token-manager";
import { AppConfig } from "../../module/data.module";

@Service({
    name: "cmsApiService"
})
@Inject("ajaxServiceFactory", "msalTokenManagerFactory", "appConfig")
class CmsApiService implements CmsTypes.ICmsApiService {

    private ajaxService: IAjaxService;

    constructor(
        private ajaxServiceFactory: IAjaxServiceFactory,
        private msalTokenManagerFactory: IMsalTokenManagerFactory,
        private appConfig: AppConfig) {

        let ajaxOptions: IAjaxServiceOptions = {
            authTokenManager: this.msalTokenManagerFactory.createInstance(this.appConfig.azureAdAppId)
        };
        
        this.ajaxService = this.ajaxServiceFactory.createInstance(ajaxOptions);
    }

    public getContentItems(cmsKeys: CmsTypes.CmsKey[]): ng.IHttpPromise<any> {
        
        return this.ajaxService.post({
            url: "/cms/api/getcontentitems",
            serviceName: "PdmsUx",
            operationName: "GetContentItems",
            dataType: "json",
            data: cmsKeys
        });
        
    }

}
