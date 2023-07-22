namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// PDMS AssetGroup schema
    /// </summary>
    public sealed class AssetGroupInfo : IAssetGroupInfo
    {
        private static readonly Policy Policy = Policies.Current;

        private const string OptionalFeatures = "OptionalFeatures";

        /// <summary>
        /// Creates a new AssetGroupInfo from the given source. 
        /// </summary>
        public AssetGroupInfo(AssetGroupInfoDocument rawData, bool enableTolerantParsing)
        {
            // Attempt to interpret the given raw data. Failure of these is fatal even if tolerant parsing is enabled.
            this.AgentId = rawData.AgentId ?? throw new InvalidOperationException("agentId may not be null");
            this.AssetGroupId = rawData.AssetGroupId ?? throw new InvalidOperationException("asset group ID may not be null");
            this.AssetGroupQualifier = rawData.AssetGroupQualifier;
            this.AssetQualifier = AssetQualifier.Parse(this.AssetGroupQualifier);
            this.IsDeprecated = rawData.IsDeprecated;
            this.MsaSiteId = rawData.MsaSiteId;
            this.AadAppId = rawData.AadAppId;
            this.ExtendedProps = rawData.ExtendedProps;

            this.VariantInfosAppliedByAgents = rawData.VariantInfosAppliedByAgents?
                .Select(x => new AssetGroupVariantInfo(x, enableTolerantParsing))
                .Cast<IAssetGroupVariantInfo>()
                .ToList();

            this.VariantInfosAppliedByPcf = rawData.VariantInfosAppliedByPcf?
                .Select(x => new AssetGroupVariantInfo(x, enableTolerantParsing))
                .Cast<IAssetGroupVariantInfo>()
                .ToList();

            // Tolerant parsing: Data Types, Capabilities/CommandTypes, Subject/Pdms Subject Types,
            // Sovereign cloud instances, deployment location and agent readiness state.
            var dataTypes = new List<DataTypeId>();
            var commandTypes = new List<PrivacyCommandType>();
            var subjectTypes = new HashSet<Common.SubjectType>();
            var supportedCloudInstances = new HashSet<CloudInstanceId>();
            var pdmsSubjectTypes = new HashSet<PdmsSubjectType>();
            var tenantIds = new HashSet<TenantId>();

            PdmsInfoParser.ParseDataTypes(rawData.DataTypes, dataTypes, enableTolerantParsing);
            PdmsInfoParser.ParseCapabilities(rawData.Capabilities, commandTypes, enableTolerantParsing);
            PdmsInfoParser.ParseSubjects(rawData.SubjectTypes, pdmsSubjectTypes, enableTolerantParsing);
            PdmsInfoParser.ParsePcfSubjects(pdmsSubjectTypes, subjectTypes, enableTolerantParsing);
            PdmsInfoParser.ParseSovereignCloudInstances(rawData.SupportedCloudInstances, supportedCloudInstances, enableTolerantParsing);
            PdmsInfoParser.ParseTenantIds(rawData.TenantIds, tenantIds, enableTolerantParsing);

            this.SupportedDataTypes = dataTypes;
            this.SupportedCommandTypes = commandTypes;
            this.PdmsSubjectTypes = pdmsSubjectTypes;
            this.SupportedSubjectTypes = subjectTypes;
            this.SupportedCloudInstances = supportedCloudInstances;
            this.TenantIds = tenantIds;

            this.AgentReadinessState = PdmsInfoParser.ParseAgentReadinessState(rawData.AgentReadiness, enableTolerantParsing);
            this.DeploymentLocation = PdmsInfoParser.ParseDeploymentLocation(rawData.DeploymentLocation, enableTolerantParsing);

            this.SupportsLowPriorityQueue = rawData.ExtendedProps != null && 
                                            rawData.ExtendedProps.ContainsKey(OptionalFeatures) &&
                                            rawData.ExtendedProps[OptionalFeatures] != null &&
                                            rawData.ExtendedProps[OptionalFeatures].Contains(Policy.OptionalFeatures.Ids.MsaAgeOutOptIn.Value);
        }

        /// <inheritdoc/>
        public AgentId AgentId { get; }

        /// <inheritdoc/>
        public AssetGroupId AssetGroupId { get; }

        /// <inheritdoc/>
        public string AssetGroupQualifier { get; }

        /// <inheritdoc/>
        public bool IsFakePreProdAssetGroup => false;

        /// <inheritdoc/>
        public IEnumerable<DataTypeId> SupportedDataTypes { get; }

        /// <inheritdoc/>
        public IEnumerable<Common.SubjectType> SupportedSubjectTypes { get; }

        /// <inheritdoc/>
        public IEnumerable<PdmsSubjectType> PdmsSubjectTypes { get; }

        /// <inheritdoc/>
        public bool IsDeprecated { get; }

        /// <inheritdoc/>
        public IEnumerable<PrivacyCommandType> SupportedCommandTypes { get; }

        /// <inheritdoc/>
        public IDictionary<string, string> ExtendedProps { get; }

        /// <inheritdoc/>
        public bool DelinkApproved => false;

        /// <summary>
        /// Is the agent ProdReady or still TestInProd
        /// </summary>
        public AgentReadinessState AgentReadinessState { get; }

        /// <inheritdoc/>
        public AssetQualifier AssetQualifier { get; }

        /// <inheritdoc/>
        public IEnumerable<TenantId> TenantIds { get; }

        /// <inheritdoc/>
        public IList<IAssetGroupVariantInfo> VariantInfosAppliedByPcf { get; }

        /// <inheritdoc/>
        public IList<IAssetGroupVariantInfo> VariantInfosAppliedByAgents { get; }

        /// <inheritdoc />
        public IEnumerable<CloudInstanceId> SupportedCloudInstances { get; }

        /// <inheritdoc />
        public CloudInstanceId DeploymentLocation { get; }

        /// <summary>
        /// The MSA site ID, if available.
        /// </summary>
        public long? MsaSiteId { get; }

        /// <summary>
        /// The AAD application ID, if available.
        /// </summary>
        public Guid? AadAppId { get; }

        /// <summary>
        /// The agent information.
        /// </summary>
        public IDataAgentInfo AgentInfo { get; internal set; }

        /// <inheritdoc />
        public bool SupportsLowPriorityQueue { get; }

        /// <summary>
        /// This is a copy of corresponding method from WhatIfAssetGroupInfo.
        /// Make sure this one has latest verified WhatIfAssetGroupInfo IsCommandActionable.
        /// </summary>
        /// <param name="command">Privacy command.</param>
        /// <param name="applicabilityResult">ApplicabilityResult applicability check results.</param>
        /// <returns>True if command is applicable.</returns>
        public bool IsCommandActionable(PrivacyCommand command, out ApplicabilityResult applicabilityResult)
        {
            DataAsset dataAsset = this.ToDataAsset();
            SignalInfo signal = command.ToSignalInfo();

            applicabilityResult = dataAsset.CheckSignalApplicability(signal);

            if (!applicabilityResult.IsApplicable())
            {
                return false;
            }

            var pcfApplicabilityResult = ApplicabilityHelper.CheckIfBlockedInPcf(command);
            if (!pcfApplicabilityResult.IsApplicable())
            {
                applicabilityResult = pcfApplicabilityResult;
                return false;
            }

            pcfApplicabilityResult = ApplicabilityHelper.CheckAgentReadiness(this, command);
            if (!pcfApplicabilityResult.IsApplicable())
            {
                applicabilityResult = pcfApplicabilityResult;
                return false;
            }

            return applicabilityResult.IsApplicable();
        }

        /// <inheritdoc/>
        public bool IsValid(out string justification)
        {
            IAssetGroupInfo assetGroupInfo = this;
            justification = "OK";
            bool isValid = true;
            string idInfo = $"AgentId/AssetGroupId: ({this.AgentId}/{this.AssetGroupId})";

            // Each asset group should have at least one capability
            if (assetGroupInfo.SupportedCommandTypes == null || !assetGroupInfo.SupportedCommandTypes.Any())
            {
                isValid = false;
                justification = "Capabilities cannot be null or empty";
            }
            else if (assetGroupInfo.PdmsSubjectTypes == null || !assetGroupInfo.PdmsSubjectTypes.Any())
            {
                isValid = false;
                justification = "SubjectTypes cannot be null or empty";
            }
            else if (assetGroupInfo.SupportedDataTypes == null || !assetGroupInfo.SupportedDataTypes.Any())
            {
                isValid = false;
                justification = "DataTypes cannot be null or empty";
            }
            else if (assetGroupInfo.AssetQualifier == null)
            {
                isValid = false;
                justification = "AssetGroupQualifier is invalid";
            }
            else if (this.DeploymentLocation != null && this.SupportedCloudInstances != null && this.SupportedCloudInstances.Any())
            {
                if (this.DeploymentLocation != Policies.Current.CloudInstances.Ids.Public)
                {
                    // Asset groups located in a sovereign cloud should only receive signals belonging to their respective cloud instance
                    if (this.SupportedCloudInstances.Count() != 1 || this.SupportedCloudInstances.First() != this.DeploymentLocation)
                    {
                        justification = $"Invalid cloud instance configuration. DeploymentLocation is {this.DeploymentLocation.Value} " +
                                             $"but SupportedCloudInstances are {string.Join(", ", this.SupportedCloudInstances.Select(s => s.ToString()))}";
                        isValid = false;
                    }
                }
            }

            if (!isValid)
            {
                justification = $"AssetGroupInfo is invalid: {justification}. {idInfo}";
            }

            return isValid;
        }
    }
}
