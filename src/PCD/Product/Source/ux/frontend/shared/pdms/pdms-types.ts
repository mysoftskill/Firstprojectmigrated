import * as SearchTypes from "../search-types";
import { SetAgentRelationshipRequest } from "../pdms-agent-relationship-types";
import { AssetGroupVariant } from "../variant/variant-types";

const useCmsHere_AuthenticationType_MsaSiteBasedAuth = "MSA site-based authentication";
const useCmsHere_AuthenticationType_AadAppBasedAuth = "AAD app-based authentication";

//  Represents data owner.
export interface DataOwner {
    id: string;
    name: string;
    description: string;
    icmConnectorId?: string;
    alertContacts: string[];
    announcementContacts: string[];
    sharingRequestContacts: string[];
    assetGroups: AssetGroup[];
    dataAgents: DataAgent[];
    writeSecurityGroups: string[];
    tagSecurityGroups?: string[];
    tagApplicationIds?: string[];
    /**
     * Service tree record. If falsy, no service tree record is associated with the data owner.
     * Entity details should not be provided, when creating/updating the data owner.
     */
    serviceTree: STEntityBase & Partial<STEntityDetailsBase>;
    hasPendingTransferRequests?: boolean;
}

//  Represents a data owner that uses Service Tree as its source for operations.
export interface STDataOwner {
    //  Data owner ID.
    id: string;
    serviceTreeId: string;
    serviceTreeIdKind: STEntityKind;
    icmConnectorId?: string;
    writeSecurityGroups: string[];
    tagSecurityGroups: string[];
    tagApplicationIds: string[];
    sharingRequestContacts: string[];
}

//  Kind of a service tree entity ID. Maps to ServiceTreeIdKind on backend.
export type STEntityKind = "service" | "teamGroup" | "serviceGroup";

//  Basic properties of service tree entity.
export interface STEntityBase {
    id: string;
    kind: STEntityKind;
}

//  Basic properties of service tree entity details.
export interface STEntityDetailsBase {
    name: string;
    description: string;
    serviceAdmins: string[];
    organizationId: string;
    divisionId: string;
}

//  Represents a Service Tree service search result entity.
export interface STServiceSearchResult extends STEntityBase {
    name: string;
}

//  Represents the detail information about a Service Tree service.
export interface STServiceDetails extends STEntityBase, STEntityDetailsBase {
}

/**
 * Type of authentication to use for data agent.
 * Value names are PascalCased to accommodate PDMS client serializer.
 */
export enum AuthenticationType {
    MsaSiteBasedAuth = 2,
    AadAppBasedAuth = 4,

}
export class AuthenticationTypeName {
    //  NOTE: Constant names must match values of AuthenticationType enum.
    public static readonly MsaSiteBasedAuth = useCmsHere_AuthenticationType_MsaSiteBasedAuth;
    public static readonly AadAppBasedAuth = useCmsHere_AuthenticationType_AadAppBasedAuth;
}

/**
 * Prod readiness state for agent.
 */
export enum AgentReadinessState {
    TestInProd = 0,
    ProdReady = 1,
}

/**
 * Release states of Data Agent Connection.
 * Value names are PascalCased to accommodate PDMS client serializer.
 */
export enum ReleaseState {
    PreProd = 0,
    Ring1 = 1,
    Ring2 = 2,
    Ring3 = 3,
    Prod = 100
}
export class ReleaseStates {
    public static readonly All = [
        ReleaseState[ReleaseState.PreProd],
        ReleaseState[ReleaseState.Ring1],
        ReleaseState[ReleaseState.Ring2],
        ReleaseState[ReleaseState.Ring3],
        ReleaseState[ReleaseState.Prod]
    ];

    public static readonly Rings = [
        ReleaseState[ReleaseState.Ring1],
        ReleaseState[ReleaseState.Ring2],
        ReleaseState[ReleaseState.Ring3]
    ];
}

//  List of well-known capability IDs, matching official NGP nomenclature.
export class PrivacyCapabilityId {
    public static readonly AccountClose = "AccountClose";
    public static readonly Delete = "Delete";
    public static readonly Edit = "Edit";
    public static readonly Export = "Export";
    public static readonly View = "View";
}

//  List of well-known data type IDs, matching official NGP nomenclature.
export class PrivacyDataTypeId {
    public static readonly BrowsingHistory = "BrowsingHistory";
    public static readonly CapturedCustomerContent = "CapturedCustomerContent";
    public static readonly CloudServiceProvider = "CloudServiceProvider";
    public static readonly CommuteAndTravel = "CommuteAndTravel";
    public static readonly CompensationAndBenefits = "CompensationAndBenefits";
    public static readonly ContentConsumption = "ContentConsumption";
    public static readonly Credentials = "Credentials";
    public static readonly CustomerContact = "CustomerContact";
    public static readonly CustomerContactList = "CustomerContactList";
    public static readonly CustomerContent = "CustomerContent";
    public static readonly DemographicInformation = "DemographicInformation";
    public static readonly DeviceConnectivityAndConfiguration = "DeviceConnectivityAndConfiguration";
    public static readonly EnvironmentalSensor = "EnvironmentalSensor";
    public static readonly FeedbackAndRatings = "FeedbackAndRatings";
    public static readonly FinancialTransactions = "FinancialTransactions";
    public static readonly FitnessAndActivity = "FitnessAndActivity";
    public static readonly InkingTypingAndSpeechUtterance = "InkingTypingAndSpeechUtterance";
    public static readonly InterestsAndFavorites = "InterestsAndFavorites";
    public static readonly LearningAndDevelopment = "LearningAndDevelopment";
    public static readonly LicensingAndPurchase = "LicensingAndPurchase";
    public static readonly MicrosoftCommunications = "MicrosoftCommunications";
    public static readonly PaymentInstrument = "PaymentInstrument";
    public static readonly PreciseUserLocation = "PreciseUserLocation";
    public static readonly ProductAndServicePerformance = "ProductAndServicePerformance";
    public static readonly ProductAndServiceUsage = "ProductAndServiceUsage";
    public static readonly Recruitment = "Recruitment";
    public static readonly SearchRequestsAndQuery = "SearchRequestsAndQuery";
    public static readonly Social = "Social";
    public static readonly SoftwareSetupAndInventory = "SoftwareSetupAndInventory";
    public static readonly SupportContent = "SupportContent";
    public static readonly SupportInteraction = "SupportInteraction";
    public static readonly WorkContracts = "WorkContracts";
    public static readonly WorkplaceInteractions = "WorkplaceInteractions";
    public static readonly WorkProfile = "WorkProfile";
    public static readonly WorkRecognition = "WorkRecognition";
    public static readonly WorkTime = "WorkTime";
}

//  List of well-known subject type IDs, matching official NGP nomenclature.
export class PrivacySubjectTypeId {
    public static readonly AADUser = "AADUser";
    public static readonly DemographicUser = "DemographicUser";
    public static readonly DeviceOther = "DeviceOther";
    public static readonly MSAUser = "MSAUser";
    public static readonly Windows10Device = "Windows10Device";
}

//  List of well-known privacy actions.
export class PrivacyActionId {
    public static readonly Delete = "Delete";
    public static readonly Export = "Export";
}

//  List of well-known protocol IDs, matching official NGP nomenclature.
export class PrivacyProtocolId {
    public static readonly CommandFeedV1 = "CommandFeedV1";
    public static readonly CosmosDeleteSignalV2 = "CosmosDeleteSignalV2";
    public static readonly CommandFeedV2Batch = "PCFV2Batch";
    public static readonly CommandFeedV2Continuous = "CommandFeedV2";
}

//  Visibility of Protocol capabilities.
export class ProtocolCapabilityVisibility {
    public static readonly AuthTypePicker = [PrivacyProtocolId.CommandFeedV1, PrivacyProtocolId.CommandFeedV2Batch, PrivacyProtocolId.CommandFeedV2Continuous];

    public static readonly MsaSiteId = [PrivacyProtocolId.CommandFeedV1];

    public static readonly AadAppId = [PrivacyProtocolId.CommandFeedV1, PrivacyProtocolId.CommandFeedV2Batch, PrivacyProtocolId.CommandFeedV2Continuous];
}

//  List of well-known cloud instance IDs.
export class PrivacyCloudInstanceId {
    public static readonly All = "All";
    public static readonly Public = "Public";
    public static readonly Fairfax = "US.Azure.Fairfax";
    public static readonly Mooncake = "CN.Azure.Mooncake";
}

export class BoundaryLocation {
    public static readonly Global = "Global";
    public static readonly EU = "EU";
}

//  Defines privacy data type.
export interface PrivacyDataType {
    //  Unique ID.
    id: string;

    //  Name (not localized).
    name: string;

    //  Description (not localized).
    description: string;

    //  A list of supported capability IDs.
    capabilities: string[];
}

//  Defines privacy capability.
export interface PrivacyCapability {
    //  Unique ID.
    id: string;

    //  Name (not localized).
    name: string;

    //  Description (not localized).
    description: string;

    //  A list of supported protocol IDs.
    protocols: string[];
}

//  Defines privacy protocol.
export interface PrivacyProtocol {
    //  Unique ID.
    id: string;

    //  Name (not localized).
    name: string;

    //  Description (not localized).
    description: string;

    //  Indicates whether the protocol is deprecated.
    isDeprecated?: boolean;
}

//  Defines privacy cloud instance.
export interface PrivacyCloudInstance {
    //  Unique ID.
    id: string;

    //  Name (not localized).
    name: string;

    //  Description (not localized).
    description: string;

    //  A list of supported cloud IDs.
    supportedClouds: string[];
}

//  Represents privacy policy for UX.
export interface PrivacyPolicy {
    //  Map of all known data types.
    dataTypes: {
        [key: string]: PrivacyDataType;
    };

    //  Map of all known capabilities.
    capabilities: {
        [key: string]: PrivacyCapability;
    };

    //  Map of all known protocols.
    protocols: {
        [key: string]: PrivacyProtocol;
    };

    //  Map of all known cloud instances.
    supportedClouds: {
        [key: string]: PrivacyCloudInstance;
    };
}

//  Defines custom data for the supported privacy actions of an asset group via data agents.
interface AssetGroupPrivacyActionsPropertyBag<TPropertyType> {
    //  Data associated with the delete agent for the asset.
    deleteAction: TPropertyType;
    //  Data associated with the export agent for the asset.
    exportAction: TPropertyType;
}

//  Defines supported/enabled states privacy actions of an asset group via data agents.
export interface PrivacyActionsState extends AssetGroupPrivacyActionsPropertyBag<boolean> { }
//  Defines tooltips for privacy actions of an asset group via data agents.
export interface PrivacyActionsTips extends AssetGroupPrivacyActionsPropertyBag<string[]> { }

//  Defines the data assets which are applicable to a data agent.
export interface DataAgentSupportedAssetGroups {
    //  The linked assets to a given agent.
    linkedAssetGroups: AssetGroup[];
    //  The unlinked assets that are supported by a given agent.
    unlinkedAssetGroups: AssetGroup[];
}

//  Defines data asset group.
export interface AssetGroup {
    //  Entity ID. Empty, if new.
    id: string;
    //  ID of the delete agent.
    deleteAgentId?: string;
    //  ID of the export agent.
    exportAgentId?: string;
    //  ID of the request for shared delete agent
    deleteSharingRequestId?: string;
    //  ID of the request for shared export agent
    exportSharingRequestId?: string;
    //  Whether there are pending variant requests or not
    hasPendingVariantRequests?: boolean;
    /**
     * Whether it's blocked from broader asset
     * groups inheritance with export agent links.
     * */
    isExportAgentInheritanceBlocked?: boolean;
    /**
     * Whether it's blocked from broader asset
     * groups inheritance with delete agent links.
     * */
    isDeleteAgentInheritanceBlocked?: boolean;
    /**
     * Whether it's blocked from broader asset
     * groups inheritance with variant links.
     * */
    isVariantsInheritanceBlocked?: boolean;
    //  ID of the data owner.
    ownerId?: string;
    //  AssetGroup qualifier.
    qualifier: AssetGroupQualifier;
    //  Variants.
    variants?: AssetGroupVariant[];
    //  ETag
    eTag?: string;
    //  Whether there are pending variant requests or not
    hasPendingTransferRequest?: boolean;
    //  Id of the recipient of the transfer request
    pendingTransferRequestTargetOwnerId?: string;
    //  Name of the recipient of the transfer request
    pendingTransferRequestTargetOwnerName?: string;
    // The optional features that associate with the asset group
    optionalFeatures?: string[];
}

export interface VariantDefinition {
    //  Id.
    id: string;
    //  Display name.
    name: string;
    //  Description.
    description: string;
    //  Owner ID.
    ownerId: string;
    //  Approver of the variant.
    approver: string;
    //  Well-known capability IDs (of PrivacyCapabilityId) from the policy to display.
    capabilities: string[];
    //  Well-known data type IDs (of PrivacyDataTypeId) from the policy to display. 
    dataTypes: string[];
    //  Well-known subject type IDs (of PrivacySubjectTypeId) from the policy to display.
    subjectTypes: string[];
}

//  Defines data asset.
export interface DataAsset {
    //  Entity ID. Empty, if new.
    id: string;
    //  AssetGroup qualifier.
    qualifier: AssetGroupQualifier;
}

//  Asset group's asset group qualifier.
export interface AssetGroupQualifier {
    //  Qualifier properties.
    props: {
        //  Well-known property - asset type ID.
        AssetType: string;
        //  Maps asset type ID to a value.
        [key: string]: string;
    };

    //  Link to find the asset in DataGrid.
    dataGridLink?: string;
}

//  Asset type description.
export interface AssetType {
    //  Predefined asset type ID.
    id: string;
    //  UI label.
    label: string;
    //  List of properties of the asset type, ordered in the way they must appear on screen.
    props: AssetTypeProperty[];
}

//  Property of asset type.
export interface AssetTypeProperty {
    //  Predefined property ID.
    id: string;
    //  UI label.
    label: string;
    //  Property description.
    description: string;
    //  Indicates whether the property must be filled by the user.
    required?: boolean;
}

//  Links to searches on DataGrid.
export interface DataGridSearch {
    //  Link to search on //datagrid
    search: string;

    //  Link to search on //datagrid/next (Office and Bing orgs).
    searchNext: string;
}

//  Response of a GetDataAssetsByAssetGroupQualifierResponse API.
export interface GetDataAssetsByAssetGroupQualifierResponse {
    //  List of found data assets.
    dataAssets: DataAsset[];

    //  Links to searches on DataGrid.
    dataGridSearch: DataGridSearch;
}

//  Connection details of data agent.
export interface DataAgentConnectionDetails {
    //  Release state of connection details.
    releaseState: string;
    //  Protocol to be used for connection, one of the PrivacyProtocolId.
    protocol: string;
    //  How connection is authenticated, one of the AuthenticationType.
    authenticationType?: string;
    //  Site ID for MSA authentication, when MSA site-based auth is used.
    msaSiteId?: number;
    //  Azure app ID, when AAD app-based auth is used.
    aadAppId?: string;
    // array of Azure app IDs, when AAD app-based auth is used.
    aadAppIds?: string[];
    //  Prod readiness state of agent.
    agentReadiness?: string;
}

//  Operational readiness checklist.
export interface OperationalReadiness {
    isLoggingEnabled: boolean;
    isLoggingCompliant: boolean;
    isLoggingIncludesCommandId: boolean;
    isReliabilityAlertsTrigger: boolean;
    isLatencyAlertsTrigger: boolean;
    isMonitoringSla: boolean;
    isScalableForCommandRate: boolean;
    isAlertsInIcm: boolean;
    isDriDocumentation: boolean;
    isDriEscalation: boolean;
    isIncidentSeverityDoc: boolean;
    isGuidesPublished: boolean;
    isE2eValidation: boolean;
    isCertExpiryAlerts: boolean;
    isCertChangeDoc: boolean;
    isServiceRecoveryPlan: boolean;
    isServiceInProd: boolean;
    isDisasterRecoveryPlan: boolean;
    isDisasterRecoveryTested: boolean;
}

//  Base structure of every data agent.
export interface DataAgentBase {
    //  Entity ID. Empty, if new.
    id: string;
    //  Display name.
    name: string;
    //  Description.
    description: string;
    //  ID of an owner.
    ownerId: string;
    //  Flag to identify if sharing is enabled for the data agent
    sharingEnabled: boolean;
    //  Flag to identify if this agent is a third party agent
    isThirdPartyAgent: boolean;
    //  Flag to identify if this agent has pending sharing requests
    hasSharingRequests?: boolean;
    //  Connection details.
    connectionDetails: DataAgentConnectionDetailsGroup;
    //  Operational readiness checklist
    operationalReadiness: OperationalReadiness;
    //  IcM connector ID.
    icmConnectorId?: string;
    //  Deployment location for the agent.
    deploymentLocation: string;
    //  Cloud instances the agent supports.
    supportedClouds: string[];
    //  Flag to maintain if Pending commands found for agent.
    pendingCommandsFound: boolean;
    //  Data boundary value
    dataResidencyBoundary: string;
}

export interface DataAgentConnectionDetailsGroup {
    [key: string]: DataAgentConnectionDetails;
}

//  Specifies delete data agent.
export interface DeleteAgent extends DataAgentBase {
    //  Type of data agent structure.
    kind: "delete-agent";

    //  Storage groups.
    assetGroups: AssetGroup[];
}

//  Specifies legacy data agent.
export interface LegacyDataAgent extends DataAgentBase {
    //  Type of data agent structure.
    kind: "legacy-agent";
}

//  Specifies PDOS data agent.
export interface PdosDataAgent extends DataAgentBase {
    // Type of data agent structure. 
    kind: "pdos-agent";
}

export interface SharingRequest {
    id: string;
    ownerId: string;
    agentId: string;
    ownerName: string;
    relationships: SharingRelationship[];
}

export interface SharingRelationship {
    assetGroupId: string;
    assetGroupQualifier: AssetGroupQualifier;
    capabilities: string[];
}

// TODO: Extract out transfer request object to their own types.ts file.
export interface TransferRequest {
    //  id of the request.
    id: string;
    //  ownerId of the original owner.
    sourceOwnerId: string;
    //  name of the original owner.
    sourceOwnerName: string;
    //  ownerId of the new owner.
    targetOwnerId: string;
    //  the state of the request, typically you shouldn't modify this.
    requestState: TransferRequestState;
    //  list of asset groups being transfered.
    assetGroups: AssetGroup[];
}

export enum TransferRequestState {
    None = 0,
    Pending = 1,
    Approving = 2,
    Approved = 3,
    Cancelled = 4,
    Failed = 5
}

export interface TrackingDetails {
    createdOn: Date;
}

export enum IcmIncidentSeverity {
    Sev2 = 2,
    Sev3 = 3,
    Sev4 = 4
}

export interface Incident {
    id?: number;
    routing: RouteData;
    severity: IcmIncidentSeverity;
    title: string;
    body: string;
    keywords: string;
}

export interface RouteData {
    ownerId: string;
    agentId: string;
    assetGroupId?: string;
}

//  All known data agent kinds (combines 'kind' types in each data agent structure).
export type DataAgentKind = "delete-agent" | "legacy-agent" | "pdos-agent";

//  Generic data agent structure.
export type DataAgent = DeleteAgent | LegacyDataAgent | PdosDataAgent;

//  Nation information when displaying a list of available nations.
export type Country = {
    //  Nation name.
    countryName: string;
    //  ISO code representing the nation.
    isoCode: string;
};

export interface ServiceTreeSelectorParent {
    service: STServiceDetails;
}

//  Provides operations on PDMS data.
export interface IPdmsDataService {
    //  Gets the data owners associated with the authenticated user.
    getOwnersByAuthenticatedUser(): ng.IPromise<DataOwner[]>;
    
    //  Gets list of nations.
    getCountriesList(): ng.IPromise<Country[]>;

    //  Gets current privacy policy.
    getPrivacyPolicy(): ng.IPromise<PrivacyPolicy>;

    /**
     * Updates data asset group record.
     * @param assetGroup Data asset group record to update.
     */
    updateAssetGroup(assetGroup: AssetGroup): ng.IPromise<AssetGroup>;

    //  Creates a new instance of a DataAgent structure.
    createNewDataAgentInstance(kind: DataAgentKind): DataAgent;

    /**
     * Gets data agent.
     * @param id Delete agent ID.
     */
    getDeleteAgentById(id: string): ng.IPromise<DataAgent>;

    /**
     * Updates data agent record.
     * @param dataAgent Data agent record to update.
     */
    updateDataAgent(dataAgent: DataAgent): ng.IPromise<DataAgent>;

    /**
     * Resets connection details in data agent to their defaults, based on selected protocol.
     * @param dataAgent Data agent instance to update.
     */
    resetDataAgentConnectionDetails(connectionDetails: DataAgentConnectionDetails): void;

    /**
     * Gets the top services that match the name substring.
     * @param nameSubstring Name to search.
     */
    getServicesByName(nameSubstring: string): ng.IPromise<STServiceSearchResult[]>;

    /**
     * Gets the detailed service information given the service ID.
     * @param id Service tree entity ID to get.
     * @param kind Service tree entity ID kind.
     */
    getServiceById(id: string, kind: STEntityKind): ng.IPromise<STServiceDetails>;

    //  Gets a data owner including Service Tree information.
    getDataOwnerWithServiceTree(ownerId: string): ng.IPromise<DataOwner>;

    /**
     * Updates data owner record.
     * @param dataOwner Data owner record to update.
     */
    updateDataOwner(dataOwner: DataOwner): ng.IPromise<DataOwner>;

    /**
     * Links existing data owner record to a service tree entity.
     * @param dataOwner Data owner record to link to service tree.
     */
    linkDataOwnerToServiceTree(dataOwner: DataOwner, serviceTreeEntity: STEntityBase): ng.IPromise<DataOwner>;

    /**
     * Deletes data owner record.
     * @param dataAgent Data owner record to delete.
     */
    deleteDataOwner(dataOwner: DataOwner): ng.IPromise<any>;

    /**
     * Deletes data agent record.
     * @param dataAgent Data agent record to delete.
     */
    deleteDataAgent(dataAgent: DataAgent): ng.IPromise<any>;

    /**
     * Deletes asset group record.
     * @param assetGroup Asset group record to delete.
     */
    deleteAssetGroup(assetGroup: AssetGroup): ng.IPromise<any>;

    /**
     * Gets asset type metadata information.
     */
    getAssetTypeMetadata(): ng.IPromise<AssetType[]>;

    /**
     * Gets asset group for an id.
     * @param id Asset group id.
     */
    getAssetGroupById(id: string): ng.IPromise<AssetGroup>;

    /**
     * Gets asset groups for a given owner id.
     * @param ownerId Data owner id.
     */
    getAssetGroupsByOwnerId(ownerId: string): ng.IPromise<AssetGroup[]>;

    /**
     * Gets asset groups count for a given owner id.
     * @param ownerId Data owner id.
     */
    getAssetGroupsCountByOwnerId(ownerId: string): ng.IPromise<number>;

    /**
     * Gets data agents count for a given owner id.
     * @param ownerId Data owner id.
     */
    getDataAgentsCountByOwnerId(ownerId: string): ng.IPromise<number>;

    /**
     * Gets asset groups for a given delete agent id.
     * @param deleteAgentId Delete agent id.
     */
    getAssetGroupsByDeleteAgentId(deleteAgentId: string): ng.IPromise<AssetGroup[]>;

    /**
     * Gets asset groups for a given agent id (export or delete).
     * @param agentId Delete agent id.
     */
    getAssetGroupsByAgentId(agentId: string): ng.IPromise<AssetGroup[]>;

    /**
     * Gets asset groups to display as being related to the agent.
     * @param agent Data agent.
     * @param ownerId Data owner id.
     */
    getAssetGroupsForAgent(agent: DataAgent, ownerId: string): ng.IPromise<DataAgentSupportedAssetGroups>;

    /**
     * Gets the variant definition for a given variant id.
     * @param variantId Variant id.
     */
    getVariantById(variantId: string): ng.IPromise<VariantDefinition>;

    /**
     * Gets data agents for a given owner id.
     * @param ownerId Data owner id.
     */
    getDataAgentsByOwnerId(ownerId: string): ng.IPromise<DataAgent[]>;

    /**
	 * gets all shared delete agents
	 */
    getSharedDataAgents(): ng.IPromise<DeleteAgent[]>;

    /**
	 * gets all data agents for a given owner Id as well as all shares agents
	 */
    getSharedDataAgentsByOwnerId(ownerId: string): ng.IPromise<DeleteAgent[]>;

    /**
     * Gets data owner by exact string match.
     * @param ownerName Data owner name to find.
     */
    getDataOwnerByName(ownerName: string): ng.IPromise<DataOwner>;

    /**
     * Gets data owner names by substring match.
     * @param ownerSubstring Substring of data owner names to find.
     */
    getDataOwnersBySubstring(ownerSubstring: string): ng.IPromise<DataOwner[]>;

    /**
     * Gets data assets by qualifier group qualifier.
     * @param assetGroupQualifier AssetGroup qualifier.
     */
    getDataAssetsByAssetGroupQualifier(assetGroupQualifier: AssetGroupQualifier): ng.IPromise<GetDataAssetsByAssetGroupQualifierResponse>;

    /**
     * Gets a sharing request.
     * @param id to locate a sharing request.
     */
    getSharingRequestById(id: string): ng.IPromise<SharingRequest>;

    /**
     * Gets all sharing requests for an agent.
     * @param agentId to locate all sharing requests associated with an agent.
     */
    getSharingRequestsByAgentId(agentId: string): ng.IPromise<SharingRequest[]>;

    /**
     * Accepts a list of sharing requests
     * @param sharingRequestIds to locate the sharing requests.
     */
    approveSharingRequests(sharingRequestIds: string[]): ng.IPromise<any>;

    /**
     * Deletes a list of sharing requests
     * @param sharingRequestIds to locate the sharing requests.
     */
    denySharingRequests(sharingRequestIds: string[]): ng.IPromise<any>;

    /**
     * Gets all transfer requests for an target owner.
     * @param ownerId to locate all transfer requests associated with a target owner.
     */
    getTransferRequestsByTargetOwnerId(ownerId: string): ng.IPromise<TransferRequest[]>;

    /**
     * Accepts a list of transfer requests
     * @param transferRequestIds to locate the transfer requests.
     */
    approveTransferRequests(transferRequestIds: string[]): ng.IPromise<any>;

    /**
     * Deletes a list of transfer requests
     * @param transferRequestIds to locate the transfer requests.
     */
    denyTransferRequests(transferRequestIds: string[]): ng.IPromise<any>;

     /**
     * Creates a transfer request to transfer a list of asset groups
     * from the source owner to a target owner.
     * @param transferRequest to be created.
     */
    createTransferRequest(transferRequest: TransferRequest): ng.IPromise<void>;

    /**
     * Sets data agent relationships with asset groups.
     * Validation on pdms side requires either all assetGroups have same ownerId
     * or all assetGroups currently assigned same agentId (applies to unlinking).
     * @param setAgentRelationshipRequest SetAgentRelationship request.
     */
    setAgentRelationshipsAsync(setAgentRelationshipRequest: SetAgentRelationshipRequest): ng.IPromise<void>;

    /**
     * Searches PDMS for terms.
     * @param terms Terms to search for.
     */
    search(terms: string): ng.IPromise<SearchTypes.SearchResults>;

     /**
     * Creates an IcM Incident
     * @param incident the incident to create
     */
    createIcmIncident(incident: Incident): ng.IPromise<Incident>;

    /**
     *  Determines if authenticated user has access to perform incident manager operations. 
     * */
    hasAccessForIncidentManager(): ng.IPromise<any>;
}
