//--------------------------------------------------------------------------------
// <copyright file="AuditData.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ifx;
    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    ///     AuditData contains MandatoryProperties, OptionalProperties and helper functions to create these properties. 
    /// </summary>
    public class AuditData
    {
        #region MandatoryProperties
        /// <summary>
        ///     Gets or sets audit categories.
        /// </summary>
        public HashSet<AuditEventCategory> AuditCategories { get; set; }

        /// <summary>
        ///     Gets or sets audit operation name.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        ///     Gets or sets audit result type.
        /// </summary>
        public OperationResult ResultType { get; set; }

        /// <summary>
        ///     Gets or sets caller identities.
        /// </summary>
        public List<CallerIdentity> CallerIdentities { get; set; }

        /// <summary>
        ///     Gets or sets target resources.
        /// </summary>
        public List<TargetResource> TargetResources { get; set; }
        #endregion

        #region OptionalProperties
        /// <summary>
        ///     Gets or sets result description.
        /// </summary>
        public string ResultDescription { get; set; }

        /// <summary>
        ///     Gets or sets caller display name.
        /// </summary>
        public string CallerDisplayName { get; set; }
        #endregion

        /// <summary>
        ///     Gets or sets extended properties.
        /// </summary>
        public AuditSchema ExtendedProperties { get; set; }

        /// <summary>
        ///     Gets or sets audit event type.
        /// </summary>
        public AuditEventType AuditEventType { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the AuditData class.
        /// </summary>
        public AuditData()
        {
            this.AuditCategories = new HashSet<AuditEventCategory>();
            this.CallerIdentities = new List<CallerIdentity>();
            this.TargetResources = new List<TargetResource>();
        }

        /// <summary>
        ///     Initializes a new instance of the AuditData class.
        /// </summary>
        /// <param name="auditEventType">Audit event type, Application or Management.</param>
        public AuditData(AuditEventType auditEventType) : this()
        {
            this.AuditEventType = auditEventType;
        }

        /// <summary>
        ///     Create MandatoryProperties using audit data.
        /// </summary>
        internal AuditMandatoryProperties CreateMandatoryProperties()
        {
            var mandatoryProperties = new AuditMandatoryProperties
            {
                OperationName = this.OperationName,
                ResultType = this.ResultType
            };

            mandatoryProperties.AddCallerIdentities(
                this.CallerIdentities.Where(c => c.CallerIdentityValue != null).ToArray());

            mandatoryProperties.AddTargetResources(
                this.TargetResources.Where(c => c.TargetResourceName != null).ToArray());

            foreach (AuditEventCategory auditCategory in this.AuditCategories)
            {
                mandatoryProperties.AddAuditCategory(auditCategory);
            }

            return mandatoryProperties;
        }

        /// <summary>
        ///     Create OptionalProperties using audit data.
        /// </summary>
        internal AuditOptionalProperties CreateOptionalProperties()
        {
            return new AuditOptionalProperties
            {
                ResultDescription = this.ResultDescription,
                CallerDisplayName = this.CallerDisplayName
            };
        }

        /// <summary>
        ///     Helper function to create AuditData instance for AccessToKeyVault operation.
        /// </summary>
        /// <param name="keyVaultBaseUrl">Base URL for the key vault.</param>
        /// <param name="name">Key name, secret name or cert name etc.</param>
        /// <param name="resultDescription">Operation result description.</param>
        /// <param name="callerDisplayName">Caller display name.</param>
        /// <param name="callerIdentities">Caller identities.</param>
        /// <param name="operationResult">Operation result.</param>
        /// <returns>An instance of the AuditData.</returns>
        public static AuditData BuildAccessToKeyVaultOperationAuditData(string keyVaultBaseUrl, string name, string resultDescription, string callerDisplayName, List<CallerIdentity> callerIdentities, OperationResult operationResult = OperationResult.Success)
        {
            return new AuditData(AuditEventType.Application)
            {
                OperationName = AuditOperation.AccessToKeyVault,
                ResultType = operationResult,
                ResultDescription = resultDescription,
                CallerIdentities = callerIdentities,
                CallerDisplayName = callerDisplayName,
                AuditCategories = new HashSet<AuditEventCategory>() { AuditEventCategory.KeyManagement },
                TargetResources = new List<TargetResource> {
                    new TargetResource("KeyVaultBaseUrl", keyVaultBaseUrl),
                    new TargetResource("Name", name)}
            };
        }

        /// <summary>
        ///     Helper function to create AuditData instance for CreateDataAgent operation.
        /// </summary>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="capabilities">Delete and/or Export.</param>
        /// <param name="ownerId">Owner ID.</param>
        /// <param name="uri">PDMS URI to create data agent.</param>
        /// <param name="resultDescription">Operation result description.</param>
        /// <param name="callerDisplayName">Caller display name.</param>
        /// <param name="callerIdentities">Caller identities.</param>
        /// <param name="operationResult">Operation result.</param>
        /// <returns>An instance of the AuditData.</returns>
        public static AuditData BuildCreateDataAgentOperationAuditData(string agentId, IEnumerable<string> capabilities, string ownerId, Uri uri, string resultDescription, string callerDisplayName, List<CallerIdentity> callerIdentities, OperationResult operationResult = OperationResult.Success)
        {
            return new AuditData(AuditEventType.Management)
            {
                OperationName = AuditOperation.CreateDataAgent,
                ResultType = operationResult,
                ResultDescription = resultDescription,
                CallerIdentities = callerIdentities,
                CallerDisplayName = callerDisplayName,
                AuditCategories = new HashSet<AuditEventCategory>() { AuditEventCategory.UserManagement },
                TargetResources = new List<TargetResource> { new TargetResource("RequestUri", uri.ToString())},
                ExtendedProperties = new DataAgentAudit
                {
                    Id = agentId,
                    Capabilities = string.Join(",", capabilities),
                    OwnerId = ownerId
                }
            };
        }
    }
}
