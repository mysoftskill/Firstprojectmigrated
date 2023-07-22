namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.PrivacyServices.KustoHelpers;

    /// <summary>
    ///     Helper to query kusto data
    /// </summary>
    public class KustoDataHelper : IKustoDataHelper
    {
        private static readonly string appId;

        private readonly IKustoConfig kustoConfig;

        private readonly IKustoClientFactory kustoFactory;

        public KustoDataHelper(IKustoClientFactory kustoClientFactory, IKustoConfig kustoConfig)
        {
            this.kustoFactory = kustoClientFactory ?? throw new ArgumentNullException(nameof(kustoClientFactory));
            this.kustoConfig = kustoConfig ?? throw new ArgumentNullException(nameof(kustoConfig));
        }

        /// <summary>
        ///     Returns a table of Agents missing ICM Connectors
        /// </summary>
        public async Task<List<Agent>> GetAgentsWithNoConnectorIdAsync()
        {
            using (IKustoClient client = this.kustoFactory.CreateClient(
                this.kustoConfig.DefaultClusterUrl,
                this.kustoConfig.DefaultDatabaseName,
                this.kustoConfig.DefaultKustoAppName))
            {
                KustoQueryOptions options;
                options = new KustoQueryOptions
                {
                    ClientRequestId = this.kustoConfig.DefaultKustoAppName + "." + Guid.NewGuid().ToString("N"),
                    ApplicationId = appId,
                    DefaultDatabase = this.kustoConfig.DefaultDatabaseName
                };

                IDataReader kustoData = await client.ExecuteQueryAsync("GetDataAgentsAndOwnersWithoutICM()", options).ConfigureAwait(false);

                var results = new List<Agent>();

                while (kustoData.Read())
                {
                    var k = new Agent
                    {
                        AgentId = kustoData.GetValue(0).ToString(),
                        AgentName = kustoData.GetValue(1).ToString(),
                        GC = kustoData.GetValue(2).ToString(),
                        AlertContacts = kustoData.GetValue(3).ToString(),
                        DivisionName = kustoData.GetValue(4).ToString(),
                        OrganizationName = kustoData.GetValue(5).ToString(),
                        ServiceGroupName = kustoData.GetValue(6).ToString(),
                        TeamGroupName = kustoData.GetValue(7).ToString(),
                        ServiceName = kustoData.GetValue(8).ToString()
                    };

                    results.Add(k);
                }

                kustoData.Dispose();
                return results;
            }
        }

        static KustoDataHelper()
        {
            string processId = Process.GetCurrentProcess().Id.ToStringInvariant();
            string machine = Environment.MachineName;
            string entry = Assembly.GetEntryAssembly()?.FullName ?? Assembly.GetCallingAssembly().FullName;
            appId = $"{machine}.{processId}.{entry}";
        }
    }
}
