namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;

#if INCLUDE_TEST_HOOKS

    /// <summary>
    /// IDataAgentMap implementation for testing
    /// </summary>
    [ExcludeFromCodeCoverage] // Justification: PPE test utility. Not coverable.
    public class HackedDataAgentMap : IDataAgentMap
    {
        private readonly IDataAgentMap innerMap;

        /// <summary>
        /// Initializes <see cref="HackedDataAgentMap"/>
        /// </summary>
        /// <param name="innerMap">DataAgentMap to initialize with</param>
        public HackedDataAgentMap(IDataAgentMap innerMap)
        {
            ProductionSafetyHelper.EnsureNotInProduction();
            this.innerMap = innerMap;
        }

        /// <inheritdoc/>
        public IDataAgentInfo this[AgentId agentId]
        {
            get
            {
                if (agentId == Config.Instance.PpeExportStressAgent.AgentId)
                {
                    return new ExportTestAgentInfo();
                }

                // Future change: To lock this down to only known agents, change this to return innerMap[agentId];
                // todo: Bug 15423435: Get rid of HackedDataAgentMap
                this.innerMap.TryGetAgent(agentId, out IDataAgentInfo innerAgent);
                return new HackedDataAgentInfo(agentId, innerAgent);
            }
        }

        public long Version => this.innerMap.Version;

        public string AssetGroupInfoStreamName => this.innerMap.AssetGroupInfoStreamName;

        public string VariantInfoStreamName => this.innerMap.VariantInfoStreamName;

        /// <inheritdoc/>
        public bool TryGetAgent(AgentId agentId, out IDataAgentInfo dataAgentInfo)
        {
            if (agentId == Config.Instance.PpeExportStressAgent.AgentId)
            {
                dataAgentInfo = new ExportTestAgentInfo();
                return true;
            }

            this.innerMap.TryGetAgent(agentId, out dataAgentInfo);
            dataAgentInfo = new HackedDataAgentInfo(agentId, dataAgentInfo);
            return true;
        }

        /// <inheritdoc/>
        public IEnumerable<AgentId> GetAgentIds()
        {
            return this.innerMap.GetAgentIds().Concat(new[] { Config.Instance.PpeExportStressAgent.AgentId });
        }

        /// <inheritdoc />
        public IEnumerable<IAssetGroupInfo> AssetGroupInfos => this.innerMap.AssetGroupInfos;

        private class HackedDataAgentInfo : IDataAgentInfo
        {
            private readonly IDataAgentInfo innerAgent;

            public HackedDataAgentInfo(AgentId agentId, IDataAgentInfo innerAgent)
            {
                this.innerAgent = innerAgent;
                this.AgentId = agentId;
            }

            public AgentId AgentId { get; }

            public bool IsOnline => this.innerAgent?.IsOnline ?? false;

            public IEnumerable<IAssetGroupInfo> AssetGroupInfos
            {
                get
                {
                    List<IAssetGroupInfo> infos = new List<IAssetGroupInfo>();
                    if (this.innerAgent != null)
                    {
                        infos.AddRange(this.innerAgent.AssetGroupInfos.Select(x => new DecoratedAssetGroupInfo(x)));
                    }

                    infos.Add(new HackedAssetGroupInfo(this));

                    return infos;
                }
            }

            public bool TryGetAssetGroupInfo(AssetGroupId assetGroupId, out IAssetGroupInfo assetGroupInfo)
            {
                if (this.innerAgent != null && this.innerAgent.TryGetAssetGroupInfo(assetGroupId, out assetGroupInfo))
                {
                    DualLogger.Instance.Information(nameof(HackedDataAgentInfo), "TryGetAssetGroupInfo -> Creating DecoratedAssetGroupInfo");
                    assetGroupInfo = new DecoratedAssetGroupInfo(assetGroupInfo);
                    return true;
                }

                DualLogger.Instance.Information(nameof(HackedDataAgentInfo), "TryGetAssetGroupInfo -> Creating HackedAssetGroupInfo");
                assetGroupInfo = new HackedAssetGroupInfo(this);
                return true;
            }

            public bool MatchesMsaSiteId(long msaSiteId)
            {
                if (this.innerAgent != null)
                {
                    return this.innerAgent.MatchesMsaSiteId(msaSiteId);
                }
                else
                {
                    // Future change: To lock this down to only known agents, change this to 'return false;'
                    // todo: Bug 15423435: Get rid of HackedDataAgentMap
                    return true;
                }
            }

            public bool MatchesAadAppId(Guid aadAppId)
            {
                if (this.innerAgent != null)
                {
                    return this.innerAgent.MatchesAadAppId(aadAppId);
                }
                else
                {
                    // Future change: To lock this down to only known agents, change this to 'return false;'
                    // todo: Bug 15423435: Get rid of HackedDataAgentMap
                    return true;
                }
            }

            public async Task MarkAsOnlineAsync()
            {
                if (this.innerAgent != null)
                {
                    await this.innerAgent.MarkAsOnlineAsync();
                }
            }

            public bool IsOptedIntoAadSubject2()
            {
                return (this.innerAgent != null) ? this.innerAgent.IsOptedIntoAadSubject2() : false;
            }

            public bool IsV2Agent()
            {
                return (this.innerAgent != null) ? this.innerAgent.IsV2Agent() : false;
            }
        }

        private class ExportTestAgentInfo : IDataAgentInfo
        {
            private readonly Dictionary<AssetGroupId, IAssetGroupInfo> assetGroupInfos;

            public ExportTestAgentInfo()
            {
                this.assetGroupInfos = Config.Instance.PpeExportStressAgent.AssetGroups.ToDictionary(
                    x => x.Id,
                    x => (IAssetGroupInfo)new ExportStressAssetGroupInfo(Config.Instance.PpeExportStressAgent.AgentId, x, this));
            }

            public AgentId AgentId => Config.Instance.PpeExportStressAgent.AgentId;

            public IEnumerable<IAssetGroupInfo> AssetGroupInfos => this.assetGroupInfos.Values;

            public bool IsOnline => true;

            public Task MarkAsOnlineAsync()
            {
                return Task.FromResult(true);
            }

            public bool MatchesAadAppId(Guid aadAppId)
            {
                return false;
            }

            public bool MatchesMsaSiteId(long msaSiteId)
            {
                return msaSiteId == Config.Instance.Common.ServiceToServiceSiteId;
            }

            public bool TryGetAssetGroupInfo(AssetGroupId assetGroupId, out IAssetGroupInfo assetGroupInfo)
            {
                return this.assetGroupInfos.TryGetValue(assetGroupId, out assetGroupInfo);
            }

            public bool IsOptedIntoAadSubject2()
            {
                return false;
            }

            public bool IsV2Agent()
            {
                return false;
            }
        }

        private class ExportStressAssetGroupInfo : IAssetGroupInfo
        {
            private readonly AgentId agentId;
            private readonly Common.ConfigGen.Configuration_PpeExportStressAgent_AssetGroups_AssetGroup configElement;

            public ExportStressAssetGroupInfo(AgentId agentId, Common.ConfigGen.Configuration_PpeExportStressAgent_AssetGroups_AssetGroup configElement, IDataAgentInfo dataAgentInfo)
            {
                this.agentId = agentId;
                this.configElement = configElement;
                this.PdmsSubjectTypes = new List<PdmsSubjectType>();
                this.ExtendedProps = new Dictionary<string, string>();
                this.AgentInfo = dataAgentInfo;
                this.SupportsLowPriorityQueue = false;
            }

            /// <inheritdoc />
            public bool SupportsLowPriorityQueue { get; }

            public IDataAgentInfo AgentInfo { get; }

            public AgentId AgentId => this.agentId;

            public AssetGroupId AssetGroupId => this.configElement.Id;

            public string AssetGroupQualifier => $"AssetType=PlatformService;Host=testexportagent.{this.configElement.ExportDataSizeMb}.{this.configElement.Id}.com ";

            public AssetQualifier AssetQualifier => AssetQualifier.Parse(this.AssetGroupQualifier);

            public bool IsFakePreProdAssetGroup => false;

            public IEnumerable<DataTypeId> SupportedDataTypes => new[] { Policies.Current.DataTypes.Ids.BrowsingHistory };

            public IEnumerable<Common.SubjectType> SupportedSubjectTypes => new[] { Common.SubjectType.Msa };

            public IEnumerable<PrivacyCommandType> SupportedCommandTypes => new[] { PrivacyCommandType.Export };

            public IList<IAssetGroupVariantInfo> VariantInfosAppliedByPcf => new IAssetGroupVariantInfo[0];

            public IList<IAssetGroupVariantInfo> VariantInfosAppliedByAgents => new IAssetGroupVariantInfo[0];

            public IEnumerable<CloudInstanceId> SupportedCloudInstances => new[] { Policies.Current.CloudInstances.Ids.Public };

            public CloudInstanceId DeploymentLocation => Policies.Current.CloudInstances.Ids.Public;

            public IEnumerable<TenantId> TenantIds => new TenantId[0];

            public bool DelinkApproved => false;

            public AgentReadinessState AgentReadinessState => AgentReadinessState.ProdReady;

            public IEnumerable<PdmsSubjectType> PdmsSubjectTypes { get; }

            public bool IsDeprecated => false;

            public IDictionary<string, string> ExtendedProps { get;  }

            public bool IsCommandActionable(PrivacyCommand command, out ApplicabilityResult applicabilityResult)
            {
                applicabilityResult = new ApplicabilityResult();
                if (!(command is ExportCommand))
                {
                    applicabilityResult.Status = ApplicabilityStatus.DoesNotApply;
                    applicabilityResult.ReasonCode = ApplicabilityReasonCode.DoesNotMatchAssetGroupCapability;
                    return false;
                }

                if (command.Subject is MsaSubject msaSubject)
                {
                    if (Config.Instance.PpeExportStressAgent.MsaPuids.Contains((ulong)msaSubject.Puid))
                    {
                        return true;
                    }
                }

                applicabilityResult.Status = ApplicabilityStatus.DoesNotApply;
                applicabilityResult.ReasonCode = ApplicabilityReasonCode.DoesNotMatchAssetGroupSubjects;
                return false;
            }

            public bool IsValid(out string justification)
            {
                justification = "OK";
                return true;
            }
        }

        /// <summary>
        /// Decorates another asset group info to apply custom PPE actionability filtering.
        /// </summary>
        private class DecoratedAssetGroupInfo : IAssetGroupInfo
        {
            private readonly IAssetGroupInfo innerAssetGroupInfo;

            public DecoratedAssetGroupInfo(IAssetGroupInfo innerAssetGroupInfo)
            {
                this.innerAssetGroupInfo = innerAssetGroupInfo;
                this.ExtendedProps = new Dictionary<string, string>();
            }

            /// <inheritdoc />
            public bool SupportsLowPriorityQueue => this.innerAssetGroupInfo.SupportsLowPriorityQueue;

            public IDataAgentInfo AgentInfo => this.innerAssetGroupInfo.AgentInfo;

            public AgentId AgentId => this.innerAssetGroupInfo.AgentId;

            public AssetGroupId AssetGroupId => this.innerAssetGroupInfo.AssetGroupId;

            public string AssetGroupQualifier => this.innerAssetGroupInfo.AssetGroupQualifier;

            public AssetQualifier AssetQualifier => this.innerAssetGroupInfo.AssetQualifier;

            public bool IsFakePreProdAssetGroup => this.innerAssetGroupInfo.IsFakePreProdAssetGroup;

            public IEnumerable<DataTypeId> SupportedDataTypes => this.innerAssetGroupInfo.SupportedDataTypes;

            public IEnumerable<Common.SubjectType> SupportedSubjectTypes => this.innerAssetGroupInfo.SupportedSubjectTypes;

            public IEnumerable<PrivacyCommandType> SupportedCommandTypes => this.innerAssetGroupInfo.SupportedCommandTypes;

            public IList<IAssetGroupVariantInfo> VariantInfosAppliedByPcf => this.innerAssetGroupInfo.VariantInfosAppliedByPcf;

            public IList<IAssetGroupVariantInfo> VariantInfosAppliedByAgents => this.innerAssetGroupInfo.VariantInfosAppliedByAgents;

            public IEnumerable<CloudInstanceId> SupportedCloudInstances => this.innerAssetGroupInfo.SupportedCloudInstances;

            public CloudInstanceId DeploymentLocation => this.innerAssetGroupInfo.DeploymentLocation;

            public IEnumerable<TenantId> TenantIds => this.innerAssetGroupInfo.TenantIds;

            public bool DelinkApproved => this.innerAssetGroupInfo.DelinkApproved;

            public AgentReadinessState AgentReadinessState => this.innerAssetGroupInfo.AgentReadinessState;

            public IEnumerable<PdmsSubjectType> PdmsSubjectTypes => this.innerAssetGroupInfo.PdmsSubjectTypes;

            public bool IsDeprecated => this.innerAssetGroupInfo.IsDeprecated;

            public IDictionary<string, string> ExtendedProps { get; }

            public bool IsCommandActionable(PrivacyCommand command, out ApplicabilityResult applicabilityResult)
            {
                return this.innerAssetGroupInfo.IsCommandActionable(command, out applicabilityResult);
            }

            public bool IsValid(out string justification)
            {
                return this.innerAssetGroupInfo.IsValid(out justification);
            }
        }

        private class HackedAssetGroupInfo : IAssetGroupInfo
        {
            private static readonly Common.SubjectType[] AllSubjectTypes = (Service.Common.SubjectType[])Enum.GetValues(typeof(Service.Common.SubjectType));
            private static readonly PrivacyCommandType[] AllCommandTypes = (PrivacyCommandType[])Enum.GetValues(typeof(PrivacyCommandType));

            public HackedAssetGroupInfo(IDataAgentInfo agentInfo)
            {
                this.AgentId = agentInfo.AgentId;
                this.AgentInfo = agentInfo;
                this.PdmsSubjectTypes = new List<PdmsSubjectType>();
                this.ExtendedProps = new Dictionary<string, string>();
            }

            /// <inheritdoc />
            public bool SupportsLowPriorityQueue => true;

            public IDataAgentInfo AgentInfo { get; }

            public AssetGroupId AssetGroupId => Config.Instance.PPEHack.FixedAssetGroupId;

            public bool IsFakePreProdAssetGroup => true;

            public string AssetGroupQualifier => "AssetType=AzureDocumentDB;AccountName=abcd";

            public AssetQualifier AssetQualifier => AssetQualifier.Parse(this.AssetGroupQualifier);

            public IEnumerable<DataTypeId> SupportedDataTypes => Policies.Current.DataTypes.Map.Keys;

            public IEnumerable<Service.Common.SubjectType> SupportedSubjectTypes => AllSubjectTypes;

            public IEnumerable<PrivacyCommandType> SupportedCommandTypes => AllCommandTypes;

            public IList<IAssetGroupVariantInfo> VariantInfosAppliedByPcf => new List<IAssetGroupVariantInfo>();

            public IList<IAssetGroupVariantInfo> VariantInfosAppliedByAgents => new List<IAssetGroupVariantInfo>();

            public IEnumerable<CloudInstanceId> SupportedCloudInstances => new[] { Policies.Current.CloudInstances.Ids.Public };

            public CloudInstanceId DeploymentLocation => Policies.Current.CloudInstances.Ids.Public;

            public IEnumerable<TenantId> TenantIds => Enumerable.Empty<TenantId>();

            public bool DelinkApproved => true;

            public AgentReadinessState AgentReadinessState => AgentReadinessState.ProdReady;

            public AgentId AgentId { get; }

            public IEnumerable<PdmsSubjectType> PdmsSubjectTypes { get; }

            public bool IsDeprecated => false;

            public IDictionary<string, string> ExtendedProps { get;  }

            public bool IsCommandActionable(PrivacyCommand command, out ApplicabilityResult applicabilityResult)
            {
                applicabilityResult = new ApplicabilityResult();
                StringBuilder commandResult = new StringBuilder("IsCommandActionable =>");
                commandResult.Append($"Command: {command.CommandId.GuidValue}, AgentId:{command.AgentId}, assetGroupId: {command.AssetGroupId} ");
                DualLogger.Instance.Information(nameof(HackedAssetGroupInfo), $"{commandResult}");

                // When we are hitting this part of the code, assetGroupId is set to 00.
                // Thus we can not check if asssociated assetGroup has optionalFeatures which has opted in for MsaAgeOut.
                // Agents are complaining that they receive ageOut commands in PPE even if they have not opted in for it.
                // returning Actionable command to false for AgeOut.
                // If agent wants to test AgeOut command they need to pass assetGroupId
                if (command.CommandType == PrivacyCommandType.AgeOut)
                {
                    return false;
                }
                return true;
            }

            public bool IsValid(out string justification)
            {
                justification = "OK";

                return true;
            }
        }
    }
#endif
}
