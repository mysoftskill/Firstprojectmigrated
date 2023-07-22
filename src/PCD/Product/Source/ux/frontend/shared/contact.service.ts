import * as angular from "angular";
import { Service, Inject } from "../module/app.module";
import * as SharedTypes from "./shared-types";

//  Kinds of admin assistance requests.
export type AdminAssistanceRequestKind = "delete-agent" | "update-prod-connection" | "move-team-assets" | "lockdown";

//  Additional assistance request properties.
export type RequestAdminAssistanceArgs = {
    //  ID of the entity request is made for.
    entityId: string;
};

//  Provides ways to contact us.
export interface IContactService {
    //  Collects user feedback via Feedback/Issues link.
    collectUserFeedback(): void;

    /**
     * Requests admin assistance to perform certain operations.
     * @param requestKind Kind of assistance request.
     * @param requestArgs Additional data for the request.
     */
    requestAdminAssistance(requestKind: AdminAssistanceRequestKind, requestArgs: RequestAdminAssistanceArgs): void;
}

//  Used for testing IContactService implementation. Do not use outside of UTs.
export interface ITestableContactService extends IContactService {
    /**
     * Formats URL using string template.
     * @param urlTemplate String template of the URL.
     * @param moreProps Property bag used to fill the template.
     */
    formatUrl(urlTemplate: string, moreProps: any): string;

    /**
     * Navigates user to the URL.
     * @param url URL to navigate to.
     */
    navigateTo(url: string): void;
}

const useCmsHere_FeedbackLinkFormat = "https://aka.ms/ngpdataagentsupport";
const useCmsHere_RequestAdminAssistanceLinkFormat = "https://aka.ms/ngpdataagentsupport";
const useCmsHere_SubjectRequestDeleteAgent = "Request to remove data agent";
const useCmsHere_SubjectRequestUpdateProdConnection = "Request to update PROD connection details";
const useCmsHere_SubjectRequestMoveTeamAssets = "Request to move all data assets and agent to another team";
const useCmsHere_SubjectRequestDuringLockdown = "Request during lockdown";

@Service({
    name: "contactService"
})
@Inject("$window", "$state")
class ContactService implements ITestableContactService {
    constructor(
        private readonly $window: ng.IWindowService,
        private readonly $state: ng.ui.IStateService) {
    }

    public collectUserFeedback(): void {
        this.navigateTo(this.formatUrl(useCmsHere_FeedbackLinkFormat, {}));
    }

    public requestAdminAssistance(requestKind: AdminAssistanceRequestKind, requestArgs: RequestAdminAssistanceArgs): void {
        let subject: string;

        switch (requestKind) {
            case "delete-agent":
                subject = useCmsHere_SubjectRequestDeleteAgent;
                break;

            case "update-prod-connection":
                subject = useCmsHere_SubjectRequestUpdateProdConnection;
                break;

            case "move-team-assets":
                subject = useCmsHere_SubjectRequestMoveTeamAssets;
                break;

            case "lockdown":
                subject = useCmsHere_SubjectRequestDuringLockdown;
                break;

            default:
                return SharedTypes.invalidConditionBreakBuild(requestKind);
        }

        this.navigateTo(this.formatUrl(useCmsHere_RequestAdminAssistanceLinkFormat, {}));
    }

    public formatUrl(urlTemplate: string, moreProps: any): string {
        let props = {
            ...moreProps,
            url: this.$state.href(this.$state.current, {}, { absolute: true }),     //  Full URL to the page user is viewing at the moment of filing feedback.
            cv: this.$window.BradburyTelemetry.cv.getCurrentCvValue()
        };

        let url = urlTemplate;
        for (let key of Object.keys(props)) {
            url = url.replace(`$${key.toLocaleUpperCase()}$`, encodeURIComponent(props[key]));
        }

        return url;
    }

    public navigateTo(url: string): void {
        this.$window.location.replace(url);
    }
}
