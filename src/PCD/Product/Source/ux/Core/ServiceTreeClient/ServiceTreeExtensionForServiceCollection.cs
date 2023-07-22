using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.ServiceTreeClient;
using Microsoft.PrivacyServices.UX.Monitoring.Events;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceTreeExtensionForServiceCollection
    {
        public static IServiceCollection AddServiceTreeClient(this IServiceCollection services)
        {
            services.AddSingleton(ParallaxConfig.Get<IServiceTreeClientConfig>("ServiceTreeClient.ini", "IServiceTreeClientConfig"));

            services.AddSingleton<PrivacyServices.DataManagement.Client.ServiceTree.IServiceTreeClient>(sp =>
            {
                var requestContextAccessor = sp.GetRequiredService<IInstrumentedRequestContextAccessor>();
                var config = sp.GetRequiredService<IServiceTreeClientConfig>();

                var monitoringHandler = new MonitoringDelegatingHandler<OutgoingServiceEvent>("ServiceTree", requestContextAccessor.GetInstrumentedRequestContext)
                {
                    //  Service Tree client adds correlation vector from request context.
                    AddCorrelationVectorHeader = false
                };

                var httpClient = HttpClientFactory.Create(monitoringHandler);
                httpClient.BaseAddress = new Uri(config.ApiEndpoint);

                return new PrivacyServices.DataManagement.Client.ServiceTree.ServiceTreeClient(new HttpProxyClient(httpClient));
            });

            services.AddTransient<IServiceTreeClientProvider, ServiceTreeClientProvider>();
            services.AddScoped<IClientProviderAccessor<IServiceTreeClientProvider>,
                ClientProviderAccessor<IServiceTreeClientProvider, MockServiceTreeClientProvider>>();

            return services;
        }
    }
}
