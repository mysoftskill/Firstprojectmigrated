import { Service, Inject } from "../../module/app.module";
import { AppConfig } from "../../module/data.module";

import { IMsalTokenManagerFactory } from "../msal-token-manager";
import { IAjaxService, IAjaxServiceFactory, IAjaxServiceOptions } from "../ajax.service";

export interface IRegistrationStatusApiService {
    //  get agent health status
    getAgentStatus(agentId: string): ng.IHttpPromise<any>;
    //  get asset group health status
    getAssetGroupStatus(assetGroupId: string): ng.IHttpPromise<any>;
}

@Service({
    name: "registrationStatusApiService"
})
@Inject("appConfig", "msalTokenManagerFactory", "ajaxServiceFactory")
class RegistrationStatusApiService implements IRegistrationStatusApiService {
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

    public getAgentStatus(agentId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "agent-status/api/getagentstatus",
            serviceName: "PdmsUx",
            operationName: "GetAgentStatus",
            data: { agentId }
        });
    }

    public getAssetGroupStatus(assetGroupId: string): ng.IHttpPromise<any> {
        return this.ajaxService.get({
            url: "agent-status/api/getassetgroupstatus",
            serviceName: "PdmsUx",
            operationName: "GetAssetGroupStatus",
            data: { assetGroupId }
        });
    }
}
