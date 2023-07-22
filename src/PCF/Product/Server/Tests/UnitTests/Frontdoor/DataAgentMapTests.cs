namespace PCF.UnitTests.Frontdoor
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using System;
    using System.Collections.Generic;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class DataAgentMapTests : INeedDataBuilders
    {
        private AgentId agentIdb663d8e9; // our PCF test agent to validate this
        private AssetGroupInfo assetGroupInfob663d8e9;
     
        [Fact]
        public void AgentCanAuthWithAadAppId()
        {
            agentIdb663d8e9 = new AgentId("b663d8e9-ab28-4d2f-afeb-c47f62f0cf36");
            var agidb663d8e9 = this.AnAssetGroupInfoDocument().Build();
            agidb663d8e9.AadAppId = new Guid("7819dd7c-2f73-4787-9557-0e342743f34b");
            assetGroupInfob663d8e9 = new AssetGroupInfo(agidb663d8e9, true);

            BuildMapAndValidateAuth();
        }

        [Fact]
        public void AgentCanAuthWithMsaSiteId()
        {
            agentIdb663d8e9 = new AgentId("b663d8e9-ab28-4d2f-afeb-c47f62f0cf36");
            var agidb663d8e9 = this.AnAssetGroupInfoDocument().Build();
            agidb663d8e9.MsaSiteId = 296170;
            assetGroupInfob663d8e9 = new AssetGroupInfo(agidb663d8e9, true);

            BuildMapAndValidateAuth();
        }

        private void BuildMapAndValidateAuth()
        {
            var agentAssetGroups = new Dictionary<AgentId, List<AssetGroupInfo>>
            {
                { agentIdb663d8e9, new List<AssetGroupInfo>() { assetGroupInfob663d8e9 } } // our PCF test agent to validate this
            };

            DataAgentMap dataAgentMap = new DataAgentMap(
                agentAssetGroups,
                123,
                "https://cosmos15.osdinfra.net/cosmos/asimov.partner.ust/shares/PXSCosmos15.Prod/PDMSPrivate/PROD/PrivacyDeleteAuditor/PCFConfig_Prod/V2/2018/10/PcfConfig_Prod_2018_10_26.ss",
                "https://cosmos15.osdinfra.net/cosmos/asimov.partner.ust/shares/PXSCosmos15.Prod/PDMSPrivate/PROD/PrivacyDeleteAuditor/PCFConfig_Prod/V2/2018/10/PcfConfig_Prod_2018_10_26.ss",
                new HashSet<AgentId>(agentAssetGroups.Keys),
                null);

            IDataAgentInfo daib663d8e9;
            
            dataAgentMap.TryGetAgent(agentIdb663d8e9, out daib663d8e9);
            Assert.True(daib663d8e9.MatchesMsaSiteId(296170));
            Assert.True(daib663d8e9.MatchesAadAppId(new Guid("7819dd7c-2f73-4787-9557-0e342743f34b")));
        }
    }
}
