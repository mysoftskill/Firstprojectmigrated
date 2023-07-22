//  UI router state params for search experience.
export interface StateParams extends ng.ui.IStateParamsService {
    //  Search terms.
    q: string;
}

//  Search results for PDMS entity.
export type SearchEntityResult = {
    //  Entity ID.
    id: string;

    //  Entity name.
    name: string;

    //  Entity description.
    description?: string;

    //  If entity is owned by the team, owner team's ID.
    ownerId?: string;
};

export type VariantRequestSearchResult = SearchEntityResult & {
    ownerName: string;
};

export type SharingRequestSearchResult = SearchEntityResult & {
    ownerName: string;
    agentId: string;
};

//  Represents section of the search results.
export type SearchResultsSection = {
    //  Indicates if the section was not retrieved due to server-side error.
    isError?: boolean;

    //  List of found entities in section.
    entities?: SearchEntityResult[];
};

//  Represents all found PDMS entities.
export type SearchResults = {
    //  List of found data owners.
    owners?: SearchResultsSection;

    //  List of found data agents.
    dataAgents?: SearchResultsSection;

    //  List of found asset groups.
    assetGroups?: SearchResultsSection;

    //  List of found variants.
    variants?: SearchResultsSection;

    // List of found variant requests.
    variantRequests?: SearchResultsSection;

    //  List of found sharing requests.
    sharingRequests?: SearchResultsSection;
};

//  Do not use: ensures that code is generated for this file (TODO: remove, once concrete types are added here).
export function __searchTypesExports() { }
