import { Service, Inject } from "../../module/app.module";
import { AgentRegistrationStatus, AssetGroupRegistrationStatus } from "./registration-status-types";
import { IRegistrationStatusApiService } from "./registration-status-api.service";

export interface IRegistrationStatusDataService {
    /**
     * get agent registration status 
     * @param agentId to retrieve registration status 
     */
    getAgentStatus(agentId: string): ng.IPromise<AgentRegistrationStatus>;
    /**
     * get asset group registration status 
     * @param assetGroupId to retrieve registration status 
     */
    getAssetGroupStatus(assetGroupId: string): ng.IPromise<AssetGroupRegistrationStatus>;
}

@Service({
    name: "registrationStatusDataService"
})
@Inject("registrationStatusApiService")
class RegistrationStatusDataService implements IRegistrationStatusDataService {
    constructor(
        private registrationStatusApiService: IRegistrationStatusApiService) { }

    //  get agent status
    public getAgentStatus(agentId: string): ng.IPromise<AgentRegistrationStatus> {
        return this.registrationStatusApiService.getAgentStatus(agentId)
            .then((result: ng.IHttpPromiseCallbackArg<AgentRegistrationStatus>) => {
                return result.data;
            });
    }

    //  get asset group status
    public getAssetGroupStatus(assetGroupId: string): ng.IPromise<AssetGroupRegistrationStatus> {
        return this.registrationStatusApiService.getAssetGroupStatus(assetGroupId)
            .then((result: ng.IHttpPromiseCallbackArg<AssetGroupRegistrationStatus>) => {
                return result.data;
            });
    }
}
