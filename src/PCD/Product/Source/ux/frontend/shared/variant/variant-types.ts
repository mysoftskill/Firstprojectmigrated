import { TrackingDetails, AssetGroupQualifier } from "../pdms/pdms-types";

export interface AssetGroupVariant {
    //  Entity ID.
    variantId: string;
    //  Entity name. This is static metadata and not kept in sync by the service.
    variantName: string;
    //  TFS tracking links.
    tfsTrackingUris: string[];
    //  Gets or sets the variant state.
    variantState: VariantState;
    //  Gets or sets the date at which this variant can no longer be used.
    variantExpiryDate?: string;
    //  Gets or sets a value indicating whether signals should be filtered for this variant.
    disabledSignalFiltering: boolean;
}

export enum VariantState {
    requested,
    approved,
    deprecated,
    rejected
}

export interface VariantRequest {
    id: string;
    ownerId: string;
    ownerName: string;
    trackingDetails: TrackingDetails;
    requestedVariants: AssetGroupVariant[];
    variantRelationships: VariantRelationship[];
}

export interface VariantRelationship {
    assetGroupId: string;
    assetGroupQualifier: AssetGroupQualifier;
}
