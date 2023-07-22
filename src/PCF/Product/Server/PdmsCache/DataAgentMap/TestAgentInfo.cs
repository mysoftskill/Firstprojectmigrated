namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// TestAgentInfo used for building agents and assetgroups for our functional tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class TestAgentInfo : IDataAgentInfo, IEnumerable<AssetGroupInfo>
    {
        private List<AssetGroupInfo> assetGroupInfos = new List<AssetGroupInfo>();

        public TestAgentInfo(AgentId agentId)
        {
            ProductionSafetyHelper.EnsureNotInProduction();
            this.AgentId = agentId;
        }

        public AgentId AgentId { get; }

        public IEnumerable<IAssetGroupInfo> AssetGroupInfos => this.assetGroupInfos;

        public bool IsOnline => true;

        public bool MatchesMsaSiteId(long msaSiteId)
        {
            return this.assetGroupInfos.Any(x => x.MsaSiteId == msaSiteId);
        }

        public bool MatchesAadAppId(Guid aadAppId)
        {
            return this.assetGroupInfos.Any(x => x.AadAppId == aadAppId);
        }

        public bool TryGetAssetGroupInfo(AssetGroupId assetGroupId, out IAssetGroupInfo assetGroupInfo)
        {
            assetGroupInfo = this.assetGroupInfos.FirstOrDefault(x => x.AssetGroupId == assetGroupId);
            return assetGroupInfo != null;
        }

        public void Add(AssetGroupInfoDocument document)
        {
            this.assetGroupInfos.Add(new AssetGroupInfo(document, false));
        }

        public IEnumerator<AssetGroupInfo> GetEnumerator()
        {
            return this.assetGroupInfos.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.assetGroupInfos.GetEnumerator();
        }

        public Task MarkAsOnlineAsync()
        {
            return Task.FromResult(true);
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
                    assetGroupInfo.ExtendedProps.TryGetValue(DataAgentMap.ProtocolPropertyKey, out var protocolValue) &&
                    (string.Compare(protocolValue, DataAgentMap.V2BatchAgentPropertyValue, StringComparison.OrdinalIgnoreCase) == 0 ||
                     string.Compare(protocolValue, DataAgentMap.V2NonBatchAgentPropertyValue, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    return true;
                }
            }

            return false;
        }
    }
}