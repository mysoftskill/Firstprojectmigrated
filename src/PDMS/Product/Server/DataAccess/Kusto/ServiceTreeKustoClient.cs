
using Microsoft.Cosmos.Management;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
using System;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto
{

    /// <summary>
    ///     ServiceTree Kusto Client
    /// </summary>
    public class ServiceTreeKustoClient : IServiceTreeKustoClient
    {
        private readonly ISessionFactory sessionFactory;
        private IKustoClient kustoClient;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KustoClientInstrumented" /> class.
        /// </summary>
        /// <param name="serviceTreeKustoConfiguration">The database client.</param>
        /// <param name="httpClient">The http client.</param>
        /// <param name="credsClient">Confidential Credentials</param>
        /// <param name="sessionFactory">The session factory</param>

        public ServiceTreeKustoClient(IServiceTreeKustoConfiguration serviceTreeKustoConfiguration, System.Net.Http.HttpClient httpClient, Azure.ComplianceServices.Common.ConfidentialCredential credsClient, ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            this.kustoClient = new KustoClient(serviceTreeKustoConfiguration, httpClient, credsClient);
        }

        /// <inheritdoc />
        public async Task<IHttpResult<KustoResponse>> QueryAsync(string query)
        {
            IHttpResult<KustoResponse> result = await sessionFactory.InstrumentAsync(
                "Kusto.Query",
                SessionType.Outgoing,
                async () => await kustoClient.QueryAsync(query));

            return result;
        }
    }
}
