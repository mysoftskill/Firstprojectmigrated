namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Net.Mail;

    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// This object is used for dynamic selection of properties from document DB.
    /// For this to work, this object must contain all properties of all entities.
    /// And the names of the properties must match the value stored in document DB 
    /// (i.e. the value in the <c>JsonProperty</c> attribute).
    /// </summary>
    public class SelectObject
    {
#pragma warning disable 1591
        public Guid id { get; set; }

        public string _etag { get; set; }

        public EntityType entityType { get; set; }

        public TrackingDetails trackingDetails { get; set; }

        public string contractVersion { get; set; }

        public bool isDeleted { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public ComplianceState complianceState { get; set; }

        public IDictionary<string, string> qualifier { get; set; }

        public IEnumerable<AssetGroupVariant> variants { get; set; }

        public Guid deleteAgentId { get; set; }

        public Guid exportAgentId { get; set; }

        public Guid deleteSharingRequestId { get; set; }

        public Guid exportSharingRequestId { get; set; }

        public Guid accountCloseAgentId { get; set; }

        public Guid inventoryId { get; set; }

        public bool isRealTimeStore { get; set; }

        public bool isVariantsInheritanceBlocked { get; set; }

        public bool isDeleteAgentInheritanceBlocked { get; set; }

        public bool isExportAgentInheritanceBlocked { get; set; }

        public bool hasPendingVariantRequests { get; set; }

        public Guid ownerId { get; set; }

        public DataCategory dataCategory { get; set; }

        public RetentionPolicy retentionPolicy { get; set; }

        public string retentionPolicyDetail { get; set; }

        public DisposalMethod disposalMethod { get; set; }

        public string disposalMethodDetail { get; set; }

        public DocumentationType documentationType { get; set; }

        public Uri documentationLink { get; set; }

        public ThirdPartyRelation thirdPartyRelation { get; set; }

        public string thirdPartyDetails { get; set; }

        public IEnumerable<DataTypeId> dataTypes { get; set; }

        public IEnumerable<SubjectTypeId> subjectTypes { get; set; }

        public string approver { get; set; }

        public Guid variantId { get; set; }

        public string egrcId { get; set; }

        public string egrcName { get; set; }

        public VariantState variantState { get; set; }

        public DateTimeOffset variantExpiryDate { get; set; }

        public VariantDefinitionState state { get; set; }

        public VariantDefinitionReason reason { get; set; }

        public IEnumerable<Uri> tfsTrackingUris { get; set; }

        public bool disableSignalFiltering { get; set; }

        public DateTimeOffset since { get; set; }

        public string data { get; set; }

        public int dataVersion { get; set; }

        public Guid? previousActiveConfigurationId { get; set; }

        public IEnumerable<MailAddress> alertContacts { get; set; }

        public IEnumerable<MailAddress> announcementContacts { get; set; }

        public IEnumerable<Guid> writeSecurityGroups { get; set; }

        public IEnumerable<Guid> tagSecurityGroups { get; set; }

        public IEnumerable<Guid> tagApplicationIds { get; set; }

        public IEnumerable<MailAddress> sharingRequestContacts { get; set; }

        public ServiceTree serviceTree { get; set; }

        public Icm icm { get; set; }

        public bool hasInitiatedTransferRequests { get; set; }

        public bool hasPendingTransferRequests { get; set; }

        public DateTimeOffset? inProdDate { get; set; }

        public EntityType derivedEntityType { get; set; }

        public IDictionary<ReleaseState, ConnectionDetail> connectionDetails { get; set; }

        public IDictionary<ReleaseState, ConnectionDetail> migratingConnectionDetails { get; set; }

        public IEnumerable<CapabilityId> capabilities { get; set; }

        public bool sharingEnabled { get; set; }

        public bool isThirdPartyAgent { get; set; }

        public DataTypeId dataType { get; set; }

        public string domain { get; set; }

        public Entity entity { get; set; }

        public WriteAction writeAction { get; set; }

        public Guid transactionId { get; set; }

        public IDictionary<Guid, SharingRelationship> relationships { get; set; }

        public string ownerName { get; set; }

        public string requesterAlias { get; set; }

        public string generalContractorAlias { get; set; }

        public string celaContactAlias { get; set; }

        public Uri workItemUri { get; set; }

        public IEnumerable<AssetGroupVariant> requestedVariants { get; set; }

        public IDictionary<Guid, VariantRelationship> variantRelationships { get; set; }

        public long operationalReadinessLow { get; set; }

        public long operationalReadinessHigh { get; set; }

        public string deploymentLocation { get; set; }

        public string dataResidencyBoundary { get; set; }

        public IEnumerable<string> supportedClouds { get; set; }

        public Guid transferRequestId { get; set; }

        public Guid sourceOwnerId { get; set; }

        public Guid targetOwnerId { get; set; }

        public Guid[] assetGroups { get; set; }

        public TransferRequestStates requestState { get; set; }

        public bool hasPendingTransferRequest { get; set; }

        public IEnumerable<OptionalFeatureId> optionalFeatures { get; set; }

        public string additionalInformation { get; set; }
#pragma warning restore 1591
    }
}