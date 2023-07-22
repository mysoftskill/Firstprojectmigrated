namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Contains an in-process and parsed version of a remote data source.
    /// </summary>
    public class DataAgentMap : IDataAgentMap
    {
        public const string ProtocolPropertyKey = "Protocol";
        public const string V2BatchAgentPropertyValue = "PCFV2Batch";
        public const string V2NonBatchAgentPropertyValue = "CommandFeedV2";

        private readonly IReadOnlyDictionary<AgentId, IDataAgentInfo> dataAgentInformationMap;

        public DataAgentMap(
            Dictionary<AgentId, List<AssetGroupInfo>> map, 
            long version, 
            string assetGroupInfoStream,
            string variantInfoStream,
            HashSet<AgentId> onlineAgents, 
            Func<AgentId, Task> setAgentAsOnlineCallback)
        {
            Dictionary<AgentId, IDataAgentInfo> agentMap = new Dictionary<AgentId, IDataAgentInfo>();

            foreach (var grouping in map)
            {
                AgentId agentId = grouping.Key;
                List<AssetGroupInfo> assetGroups = grouping.Value;

                DataAgentInfo dataAgentInfo = new DataAgentInfo(agentId, assetGroups, onlineAgents.Contains(agentId), setAgentAsOnlineCallback);
                agentMap[dataAgentInfo.AgentId] = dataAgentInfo;

                foreach (AssetGroupInfo assetGroup in assetGroups)
                {
                    assetGroup.AgentInfo = dataAgentInfo;
                }
            }
  
            this.dataAgentInformationMap = agentMap;
            this.Version = version;

            this.AssetGroupInfoStreamName = GetCosmosStreamNameFromPath(assetGroupInfoStream);
            this.VariantInfoStreamName = GetCosmosStreamNameFromPath(variantInfoStream);
        }

        public IDataAgentInfo this[AgentId agentId] => this.dataAgentInformationMap[agentId];

        public long Version { get; }

        public string AssetGroupInfoStreamName { get; }

        public string VariantInfoStreamName { get; }

        public bool TryGetAgent(AgentId agentId, out IDataAgentInfo dataAgentInfo)
        {
            return this.dataAgentInformationMap.TryGetValue(agentId, out dataAgentInfo);
        }
        
        public IEnumerable<AgentId> GetAgentIds()
        {
            return this.dataAgentInformationMap.Keys;
        }

        /// <inheritdoc />
        public IEnumerable<IAssetGroupInfo> AssetGroupInfos
        {
            get
            {
                return this.dataAgentInformationMap.Values.Select(x => x.AssetGroupInfos).SelectMany(y => y).Distinct();
            }
        }

        private class DataAgentInfo : IDataAgentInfo
        {
            private readonly Dictionary<AssetGroupId, AssetGroupInfo> assetGroupInfos;

            // Tracks the known AAd authentication results for this agent to avoid a linear scan of all asset groups per each request.
            // Similar purpose to msaSiteIdAuthResults
            private readonly ConcurrentDictionary<Guid, bool> aadAppIdAuthResults;
            private readonly ConcurrentDictionary<long, bool> msaSiteIdAuthResults;
            private readonly Func<AgentId, Task> setAsOnlineAsync;

            /// <summary>
            /// This is a temporary workaround to enable SC agents using MSA auth
            /// to move to AAD auth, without changing PCD and PDMS to support agents to use multiple 
            /// Authentication tokens.
            /// </summary>
            private static Dictionary<AgentId, (Guid aadApplicationId, long msaSiteId)> agentsSwitchingAuthentication = 
                new Dictionary<AgentId, (Guid aadApplicationId, long msaSiteId)>
                {
                    { new AgentId("b663d8e9-ab28-4d2f-afeb-c47f62f0cf36"), (new Guid("7819dd7c-2f73-4787-9557-0e342743f34b"), 296170) } // our PCF test agent to validate this
                };

            /// <summary>
            /// This is a temporary workaround to enable agent using AAD auth to support more than one App Id
            /// without changing PCD and PDMS.
            /// </summary>
            private static Dictionary<AgentId, Guid> aadAgentsAppIdAlternate =
                new Dictionary<AgentId, Guid>
                {
                    { new AgentId("b663d8e9-ab28-4d2f-afeb-c47f62f0cf36"), new Guid("fb9f9d15-8fd7-4495-850f-8f5cb676555a") }, // our PCF test agent to validate this
                    { new AgentId("0026f6f1-9444-4c2f-8183-587d0f26f5bd"), new Guid("675079ac-3499-40b8-8e0f-1bb0b81abd03") }
                };

            public DataAgentInfo(AgentId agentId, IEnumerable<AssetGroupInfo> assetGroupInfos, bool isOnline, Func<AgentId, Task> setAsOnlineAsync)
            {
                this.msaSiteIdAuthResults = new ConcurrentDictionary<long, bool>();
                this.aadAppIdAuthResults = new ConcurrentDictionary<Guid, bool>();

                this.assetGroupInfos = assetGroupInfos.ToDictionary(x => x.AssetGroupId, x => x);
                this.AgentId = agentId;
                this.IsOnline = isOnline;
                this.setAsOnlineAsync = setAsOnlineAsync;

                this.AddAlternateAuthenticationIds();
            }

            public AgentId AgentId { get; }

            public IEnumerable<IAssetGroupInfo> AssetGroupInfos => this.assetGroupInfos.Values;

            public bool IsOnline { get; private set; }

            public bool TryGetAssetGroupInfo(AssetGroupId assetGroupId, out IAssetGroupInfo assetGroupInfo)
            {
                bool result = this.assetGroupInfos.TryGetValue(assetGroupId, out AssetGroupInfo info);
                assetGroupInfo = info;
                return result;
            }

            /// <inheritdoc/>
            public bool MatchesAadAppId(Guid aadAppIdLocal)
            {
                return this.CheckAuthorized(this.aadAppIdAuthResults, aadAppIdLocal, info => info.AadAppId);
            }

            /// <inheritdoc/>
            public bool MatchesMsaSiteId(long msaSiteIdLocal)
            {
                return this.CheckAuthorized(this.msaSiteIdAuthResults, msaSiteIdLocal, info => info.MsaSiteId);
            }

            public async Task MarkAsOnlineAsync()
            {
                if (this.IsOnline)
                {
                    return;
                }

                await this.setAsOnlineAsync(this.AgentId);
                this.IsOnline = true;
            }

            public bool IsOptedIntoAadSubject2()
            {
                foreach (var assetGroupInfo in AssetGroupInfos)
                {
                    if (assetGroupInfo.SupportedSubjectTypes.Contains(SubjectType.Aad2))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsV2Agent()
            {
                foreach (var assetGroupInfo in AssetGroupInfos)
                {
                    if (assetGroupInfo.ExtendedProps != null &&
                        assetGroupInfo.ExtendedProps.TryGetValue(ProtocolPropertyKey, out var protocolValue) &&
                        (string.Compare(protocolValue, V2BatchAgentPropertyValue, StringComparison.OrdinalIgnoreCase) == 0 ||
                         string.Compare(protocolValue, V2NonBatchAgentPropertyValue, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool CheckAuthorized<T>(ConcurrentDictionary<T, bool> authorizations, T testValue, Func<AssetGroupInfo, T?> getValue) where T : struct
            {
                if (authorizations.TryGetValue(testValue, out bool authorized))
                {
                    if (!authorized)
                    {
                        DualLogger.Instance.Warning(nameof(DataAgentMap), $"Authorized ID {testValue} does not match known App ID for agent {this.AgentId}.");
                    }

                    return authorized;
                }

                authorized = this.assetGroupInfos.Values.Any(x => getValue(x) != null && getValue(x).Value.Equals(testValue));
                authorizations[testValue] = authorized;
                return authorized;
            }

            /// <summary>
            /// This is a temporary workaround to enable SC agents using MSA auth
            /// to move to AAD auth, without changing PCD and PDMS to support agents to use multiple 
            /// Authentication tokens.
            /// </summary>
            private void AddAlternateAuthenticationIds()
            {
                if (agentsSwitchingAuthentication.ContainsKey(this.AgentId))
                {
                    this.msaSiteIdAuthResults[agentsSwitchingAuthentication[this.AgentId].msaSiteId] = true;
                    this.aadAppIdAuthResults[agentsSwitchingAuthentication[this.AgentId].aadApplicationId] =  true;
                }

                if (aadAgentsAppIdAlternate.ContainsKey(this.AgentId))
                {
                    this.aadAppIdAuthResults[aadAgentsAppIdAlternate[this.AgentId]] = true;
                }
            }
        }

        private static string GetCosmosStreamNameFromPath(string streamPath)
        {
            var nameStartIndex = streamPath.LastIndexOf("/") + 1;
            var nameEndIndex = streamPath.IndexOf("?");
            if(nameEndIndex != -1)
            {
                return streamPath.Substring(nameStartIndex, nameEndIndex - nameStartIndex);
            }
            else
            {
                return streamPath.Substring(nameStartIndex);
            }
        }
    }
}
