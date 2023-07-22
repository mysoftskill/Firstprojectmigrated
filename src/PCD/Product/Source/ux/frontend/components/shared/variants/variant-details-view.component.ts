import { Component } from "../../../module/app.module";
import template = require("./variant-details-view.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";
import { StringUtilities } from "../../../shared/string-utilities";

const useCmsHere_ImplicitDefaultVariantValue = "All";
const useCmsHere_DisabledSignalFiltering = "Disabled - Signals will not be filtered from your data agent, however the variant will still be linked to your data assets.";
const useCmsHere_EnabledSignalFiltering = "Enabled - Signals will be filtered from your data agent in accordance with the variant capabilities.";

/**
 * Display strings corresponding to PrivacyCapabilityId that should NOT be localized
 * until NGP nomenclature is also localized.
 */
const useCmsHere_CapabilityStrings: { [privacyCapabilityId: string]: string } = {
    AccountClose: "Account Close",
    Delete: "Delete",
    Export: "Export"
};

/**
 * Display strings corresponding to PrivacyDataTypeId that should NOT be localized
 * until NGP nomenclature is also localized.
 */
const useCmsHere_DataTypeStrings: { [privacyDataTypeId: string]: string } = {
    BrowsingHistory: "Browsing History",
    CapturedCustomerContent: "Captured Customer Content",
    CloudServiceProvider: "Cloud Service Provider",
    CommuteAndTravel: "Commute And Travel",
    CompensationAndBenefits: "Compensation And Benefits",
    ContentConsumption: "Content Consumption",
    Credentials: "Credentials",
    CustomerContact: "Customer Contact",
    CustomerContactList: "Customer Contact List",
    CustomerContent: "Customer Content",
    DemographicInformation: "Demographic Information",
    DeviceConnectivityAndConfiguration: "Device Connectivity And Configuration",
    EnvironmentalSensor: "Environmental Sensor",
    FeedbackAndRatings: "Feedback And Ratings",
    FinancialTransactions: "Financial Transactions",
    FitnessAndActivity: "Fitness And Activity",
    InkingTypingAndSpeechUtterance: "Inking Typing And Speech Utterance",
    InterestsAndFavorites: "Interests And Favorites",
    LearningAndDevelopment: "Learning And Development",
    LicensingAndPurchase: "Licensing And Purchase",
    MicrosoftCommunications: "Microsoft Communications",
    PaymentInstrument: "Payment Instrument",
    PreciseUserLocation: "Precise User Location",
    ProductAndServicePerformance: "Product And Service Performance",
    ProductAndServiceUsage: "Product And Service Usage",
    Recruitment: "Recruitment",
    SearchRequestsAndQuery: "Search Requests And Query",
    Social: "Social",
    SoftwareSetupAndInventory: "Software Setup And Inventory",
    SupportContent: "Support Content",
    SupportInteraction: "Support Interaction",
    WorkContracts: "Work Contracts",
    WorkplaceInteractions: "Workplace Interactions",
    WorkProfile: "Work Profile",
    WorkRecognition: "Work Recognition",
    WorkTime: "Work Time"    
};

/**
 * Display strings corresponding to PrivacySubjectTypeId that should NOT be localized
 * until NGP nomenclature is also localized.
 */
const useCmsHere_SubjectTypeStrings: { [privacySubjectTypeId: string]: string } = {
    AADUser: "AAD User",
    DemographicUser: "Demographic User",
    DeviceOther: "Device Other",
    MSAUser: "MSA User",
    Windows10Device: "Windows 10 Device"
};

@Component({
    name: "pcdVariantDetailsView",
    options: {
        template,
        bindings: {
            qualifier: "<pcdQualifier",
            variantDefinition: "<pcdVariantDefinition",
            tfsTrackingUris: "<?pcdTfsTrackingUris",
            disabledSignalFiltering: "<?pcdDisabledSignalFiltering"
        }
    }
})
export default class VariantDetailsView implements ng.IComponentController {
    public qualifier: Pdms.AssetGroupQualifier;
    public variantDefinition: Pdms.VariantDefinition;
    public tfsTrackingUris: string[];
    public disabledSignalFiltering: boolean;

    private readonly dateTimeFormatter = new Intl.DateTimeFormat();

    public isReady(): boolean {
        return !!this.qualifier && !!this.variantDefinition;
    }

    public getCapabilityDisplayString(): string {
        return StringUtilities.getCommaSeparatedList(this.variantDefinition.capabilities, useCmsHere_CapabilityStrings, useCmsHere_ImplicitDefaultVariantValue);
    }

    public getDataTypeDisplayString(): string {
        return StringUtilities.getCommaSeparatedList(this.variantDefinition.dataTypes, useCmsHere_DataTypeStrings, useCmsHere_ImplicitDefaultVariantValue);
    }

    public getSubjectTypeDisplayString(): string {
        return StringUtilities.getCommaSeparatedList(this.variantDefinition.subjectTypes, useCmsHere_SubjectTypeStrings, useCmsHere_ImplicitDefaultVariantValue);
    }

    public getLinkSuffix(index: number): string {
        return this.tfsTrackingUris.length > 1 ? (index + 1).toString() : "";
    }

    public getDisabledSignalFilteringDisplayString(): string {
        return this.disabledSignalFiltering ? useCmsHere_DisabledSignalFiltering : useCmsHere_EnabledSignalFiltering;
    }
}
