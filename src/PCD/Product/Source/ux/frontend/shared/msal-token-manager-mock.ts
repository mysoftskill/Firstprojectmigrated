import { Config, Inject } from "../module/app.module";

import { IMsalTokenManager, IMsalTokenManagerFactory } from "./msal-token-manager";
import { IMocksService } from "./mocks.service";

/**
 * Mocks Adal token manager.
 */
class MsalTokenManagerMock implements IMsalTokenManager {
    @Config()
    @Inject("$provide")
    public static configureMsalTokenManagerFactoryMock($provide: ng.auto.IProvideService): void {
        $provide.decorator("msalTokenManagerFactory", ["$delegate", "$q", "mocksService", (
            $delegate: IMsalTokenManagerFactory,
            $q: ng.IQService,
            mocksService: IMocksService
        ): IMsalTokenManagerFactory => {
            return !mocksService.isActive() ? $delegate :
                <IMsalTokenManagerFactory> {
                    createInstance: (resource: string): IMsalTokenManager => {
                        return new MsalTokenManagerMock($delegate.createInstance(resource), $q, mocksService);
                    }
                };
        }]);
    }

    constructor(
        private readonly real: IMsalTokenManager,
        private readonly $q: ng.IQService,
        private readonly mocksService: IMocksService
    ) { }

    public getTokenAsync(): ng.IPromise<string> {
        if (this.mocksService.getCurrentMode() === "i9n") {
            //  Returning back mocked token as the WebRole does not care about auth. Also to avoid
            //  showing auth warning banner. 
            return this.$q.resolve("I9nMode_InvalidToken");
        }

        return this.mocksService.getScenarios().indexOf("unauthenticated") > -1 ?
            this.$q.reject("Mock unauth") : this.real.getTokenAsync();
    }
}