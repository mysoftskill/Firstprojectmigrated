import { Service, Inject } from "../../module/app.module";
import { IAjaxService, IAjaxServiceFactory, IAjaxServiceOptions } from "../ajax.service";
import { AppConfig } from "../../module/data.module";
import { IMsalTokenManagerFactory } from "../msal-token-manager";

//  API for ground control service.
export interface IGroundControlApiService {
    //  Gets all flights the user is rolled into.
    getUserFlights(): ng.IHttpPromise<any>;
}

@Service({
    name: "groundControlApiService"
})
@Inject("appConfig", "msalTokenManagerFactory", "ajaxServiceFactory")
class GroundControlApiService implements IGroundControlApiService {
    private ajaxService: IAjaxService;

    constructor(
        private readonly appConfig: AppConfig,
        private readonly msalTokenManagerFactory: IMsalTokenManagerFactory,
        private readonly ajaxServiceFactory: IAjaxServiceFactory
    ) {
        let ajaxOptions: IAjaxServiceOptions = {
            authTokenManager: msalTokenManagerFactory.createInstance(this.appConfig.azureAdAppId)
        };
        this.ajaxService = ajaxServiceFactory.createInstance(ajaxOptions);
    }

    public getUserFlights(): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "/api/getuserflights",
            serviceName: "PdmsUx",
            operationName: "GetUserFlights",
            cache: true,
            maxRetry: 0,
            timeout: 3000   //  msec.
        });
    }
}
