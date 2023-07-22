import { Service, Inject } from "../../module/app.module";
import { IGroundControlApiService } from "./ground-control-api.service";
import { StringUtilities } from "../string-utilities";

//  Data service for ground control.
export interface IGroundControlDataService {
    /**
     * Initializes service for current user. This is a best-effort call that allows to frontload querying
     * of flights as early as possible in application lifecycle. If it fails, the failure will be
     * swallowed, and calls to other method of this contract will attempt to re-query flights, until
     * one of the attempts succeeds.
     */
    initializeForCurrentUser(): ng.IPromise<void>;

    /**
     * Checks if the user is in a flight with particular name.
     * @param flightName Name of the flight to test.
     */
    isUserInFlight(flightName: string): ng.IPromise<boolean>;
}

/**
 *  Testable version of IGroundControlDataService. 
 **/
export interface ITestableGroundControlDataService extends IGroundControlDataService {
    /** 
     * Resets service state before running test. 
     **/
    resetState(): void;
}

@Service({
    name: "groundControlDataService"
})
@Inject("groundControlApiService", "$q")
class GroundControlDataService implements ITestableGroundControlDataService {
    private initializationPromise: ng.IPromise<void>;
    private currentUserFlights: string[];

    constructor(
        private readonly groundControlApiService: IGroundControlApiService,
        private readonly $q: ng.IQService) {
    }

    public initializeForCurrentUser(): ng.IPromise<void> {
        if (this.initializationPromise) {
            throw new Error("The service was initialized earlier.");
        }

        return this.getOrCreateInitializationPromise();
    }

    public isUserInFlight(flightName: string): ng.IPromise<boolean> {
        return this.getOrCreateInitializationPromise()
            .then(() => _.any(this.currentUserFlights, userFlight => StringUtilities.areEqualIgnoreCase(userFlight, flightName)));
    }

    public resetState(): void {
        delete this.initializationPromise;
        this.currentUserFlights = [];
    }

    private getOrCreateInitializationPromise(): ng.IPromise<void> {
        if (this.initializationPromise) {
            return this.initializationPromise;
        }

        return this.initializationPromise = this.groundControlApiService.getUserFlights()
            .then((response: ng.IHttpPromiseCallbackArg<string[]>) => {
                this.currentUserFlights = response.data || [];
            })
            .catch(() => {
                //  Swallow the failure. If we were not able to retrieve list of flights, assume
                //  the user is not in any flight. Failing calls are monitored on backend and
                //  will be addressed as needed.
                this.resetState();

                console.warn("Unable to retrieve list of flights. Assume no flights are active.");

                return this.$q.resolve();
            });
    }
}
