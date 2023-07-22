using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Monitoring.Events;
using Microsoft.Osgs.Core.Helpers;

namespace Microsoft.PrivacyServices.UX.Core.UhfClient
{
    public sealed class UhfHttpClient : IUhfHttpClient
    {
        public UhfHttpClient(IUhfClientConfig config, IInstrumentedRequestContextAccessor requestContextAccessor)
        {
            EnsureArgument.NotNull(config, nameof(config));
            EnsureArgument.NotNull(requestContextAccessor, nameof(requestContextAccessor));

            var monitoringHandler = new MonitoringDelegatingHandler<OutgoingServiceEvent>("Uhf", requestContextAccessor.GetInstrumentedRequestContext);

            HttpClient = HttpClientFactory.Create(monitoringHandler);
            HttpClient.BaseAddress = new Uri(config.ServiceEndpoint);
            HttpClient.DefaultRequestHeaders.Add("user-agent", config.UserAgent);
        }

        #region IUhfHttpClient Members

        public HttpClient HttpClient { get; }

        #endregion
    }
}
