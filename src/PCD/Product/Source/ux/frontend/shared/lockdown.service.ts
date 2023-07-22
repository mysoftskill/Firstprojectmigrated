import { Service, Inject } from "../module/app.module";
import { AppConfig } from "../module/data.module";
import { IStringFormatFilter } from "./filters/string-format.filter";

const useCmsHere_LockdownNotificationMessageFormat = "NGP Common Infra is in lockdown from {0} until {1} and some functionality will not be available. Please contact us, if you need to make changes in PROD configuration during this period of time.";

//  Provides facilities for supporting lockdown across NGP.
export interface ILockdownService {
    //  Returns value indicating whether the lockdown is active.
    isActive(): boolean;

    //  Gets a message that should be displayed to user, when lockdown is active. Returns empty string if lockdown is not active.
    getMessage(): string;
}

@Service({
    name: "lockdownService"
})
@Inject("appConfig", "stringFormatFilter")
class LockdownService implements ILockdownService {
    constructor(
        private readonly appConfig: AppConfig,
        private readonly stringFormatFilter: IStringFormatFilter) {
    }

    public isActive(): boolean {
        return !!this.appConfig.lockdown && this.appConfig.lockdown.isActive;
    }

    public getMessage(): string {
        if (!this.isActive()) {
            return "";
        }

        //  NOTE: the configuration is coming from the webrole and we trust the date values are correct.
        let startDate = new Date(this.appConfig.lockdown.startedUtc);
        let endDate = new Date(this.appConfig.lockdown.endedUtc);
        let dateTimeFormatter = new Intl.DateTimeFormat();

        return this.stringFormatFilter(useCmsHere_LockdownNotificationMessageFormat, [dateTimeFormatter.format(startDate), dateTimeFormatter.format(endDate)]);
    }
}
