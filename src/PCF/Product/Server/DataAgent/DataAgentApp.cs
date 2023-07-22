namespace Microsoft.PrivacyServices.CommandFeed.Service.DataAgent
{
    using System.Collections.Generic;
    using System.Net;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation;

    public sealed class DataAgentApp : PrivacyApplication
    {
        private List<PcfDataAgent> pcfDataAgents = new List<PcfDataAgent>();

        private const string AadAppIdProd = "7819dd7c-2f73-4787-9557-0e342743f34b";
        private const string AadAppIdPpe = "fb9f9d15-8fd7-4495-850f-8f5cb676555a";

        // PCF Command Feed Test Data Agent
        private readonly AgentId dataAgentId = new AgentId("b663d8e9-ab28-4d2f-afeb-c47f62f0cf36");

        /// <summary>
        /// Run data agent app
        /// </summary>
        public static void Main(string[] args)
        {
            var dataAgentApp = new DataAgentApp();
            dataAgentApp.Run(args);
        }

        private DataAgentApp()
            : base(CommandFeedService.DataAgent)
        {
            PcfDataAgent dataAgent;

            // PCF PPE DataAgent
            dataAgent = new PcfDataAgent(
                agentId: dataAgentId.GuidValue,
                aadAppId: AadAppIdPpe,
                endpointConfig: CommandFeedEndpointConfiguration.Preproduction);

            pcfDataAgents.Add(dataAgent);

            // PCF Prod DataAgent
            dataAgent = new PcfDataAgent(
                agentId: dataAgentId.GuidValue,
                aadAppId: AadAppIdProd,
                endpointConfig: CommandFeedEndpointConfiguration.Production);

            pcfDataAgents.Add(dataAgent);
        }

        /// <inheritdoc />
        protected override void OnStart()
        {
            Logger.Instance = new SllLogger();
            ServicePointManager.DefaultConnectionLimit = 500;

            foreach (var da in this.pcfDataAgents)
            {
                this.AddTask(da.RunAsync(this.CancellationToken));
            }
        }
    }
}
