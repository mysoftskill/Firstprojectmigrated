namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    public class KustoClientInstrumented : IKustoClient
    {
        private readonly ISessionFactory sessionFactory;
        private readonly IKustoClient client;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KustoClientInstrumented" /> class.
        /// </summary>
        /// <param name="kustoConfig">The database client.</param>
        /// <param name="httpClient">The http client.</param>
        /// <param name="credsClient">Confidential Credentials</param>
        /// <param name="sessionFactory">The session factory</param>
        public KustoClientInstrumented(IKustoClientConfig kustoConfig,
            HttpClient httpClient, ConfidentialCredential credsClient, ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            client = new KustoClient(kustoConfig, httpClient, credsClient);
        }

        /// <inheritdoc />
        public async Task<IHttpResult<KustoResponse>> QueryAsync(string query)
        {
            IHttpResult<KustoResponse> result = await sessionFactory.InstrumentAsync(
                "Kusto.Query",
                SessionType.Outgoing,
                async () => await client.QueryAsync(query));

            return result;
        }
    }
}