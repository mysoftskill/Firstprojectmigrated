import { AssetGroupQualifier, ReleaseState, DeleteAgent, DataOwner } from "../pdms/pdms-types";

export interface AgentRegistrationStatus {
    //  The id of the agent.
    id: string;
    //  The owner id of the agent.
    ownerId: string;
    //  An overall summary of whether or not the agent registration is complete.
    isComplete: boolean;
    //  The set of protocols for this agent.
    protocols: string[];
    //  The protocol registration status.
    protocolStatus: RegistrationState;
    //  The set of environments that the agent connection details target.
    environments: ReleaseState[];
    //  The environment registration status.
    environmentStatus: RegistrationState;
    //  The set of capabilities for this agent.
    capabilities: string[];
    //  The capability registration status.
    capabilityStatus: RegistrationState;
    //  The set of asset group registration statuses for all asset groups linked to this agent.
    assetGroups: AssetGroupRegistrationStatus[];
    //  An overall summary of whether or not all asset group registrations are correct.
    assetGroupsStatus: RegistrationState;
}

export enum RegistrationState {
    //  Indicates an invalid registration.
    invalid = "Invalid",
    //  Indicates a valid registration.
    valid = "Valid",
    //  Indicates something is deprecated.
    deprecated = "Deprecated",
    //  Indicates something is missing.
    missing = "Missing",
    //  Indicates something is only partially correct.
    partial = "Partial",
    /** Indicates a valid registration but results were truncated because they were too
        large. As such, there may be other invalid data that is hidden. */
    validButTruncated = "ValidButTruncated",
    //  Indicates a registration that is not subject to DSR requests.
    notApplicable = "NotApplicable"
}

export interface AssetGroupRegistrationStatus {
    //  The id of the asset group.
    id: string;

    //  The owner id of the asset group.
    ownerId: string;

    //  The owner name of the asset group.
    ownerName: string;

    //  An overall summary of whether or not the asset group registration is complete.
    isComplete: boolean;

    //  The qualifier for the asset group.
    qualifier: AssetGroupQualifier;

    //  The set of asset registration statuses for all assets linked to this asset group.
    assets: AssetRegistrationStatus[];

    //  An overall summary of whether or not all asset registrations are correct.
    assetsStatus: RegistrationState;
}

export interface AssetRegistrationStatus {
    //  The id of the asset from DataGrid.
    id: string;

    //  An overall summary of whether or not the asset registration is complete.
    isComplete: boolean;

    //  The qualifier for the asset.
    qualifier: AssetGroupQualifier;

    //  Whether or not the NonPersonal tag was found.
    isNonPersonal: boolean;

    //  Whether or not the LongTail or CustomNonUse tag was found.
    isLongTailOrCustomNonUse: boolean;

    //  The subject type tags for the asset.
    subjectTypeTags: Tag[];

    //  The subject type tag registration status.
    subjectTypeTagsStatus: RegistrationState;

    //  The data type tag registration status.
    dataTypeTags: Tag[];

    //  The data type tag registration status.
    dataTypeTagsStatus: RegistrationState;
}

// One of a set of predefined values that can be applied to a DataAsset.
export interface Tag {
    //  The tag name. Must be unique across tags.
    name: string;
}

//  Various Health status icons displayed on UI
export enum HealthIcon {
    //  Request to fetch agent health status is pending.
    pending = "Pending",
    //  Agent health status loaded and its healthy.
    healthy = "Healthy",
    //  Agent health status loaded and its unhealthy.
    unhealthy = "Unhealthy",
    //  health status is not determined
    incomplete = "Incomplete",
    //  Request to fetch agent health status failed.
    error = "Error",
    //  health status is unknown
    unknown = "Unknown"
}

//  Delete Agent with Health Status
export interface DataAgentWithHealthStatus {
    //  Agent
    agent: DeleteAgent;
    //  Owner of agent
    owner: DataOwner;
    //  Agent health status.
    agentHealthIcon: HealthIcon;
    //  Agent health details.
    registrationStatus: AgentRegistrationStatus;
}
