// TODO move this to /types folder
export class SetAgentRelationshipRequest {
    relationships: Relationship[];
}

export interface Relationship {
    assetGroupId: string;
    assetGroupETag: string;
    actions: Action[];
}

export interface Action {
    capability: Capability;
    agentId: string;
    verb: ActionVerb;
}

export enum Capability {
    delete,
    export
}

export enum ActionVerb {
    set,
    clear
}
