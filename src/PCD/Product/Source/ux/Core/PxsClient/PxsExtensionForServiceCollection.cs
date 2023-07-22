using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.Osgs.Infra.Platform;
using Microsoft.PrivacyServices.PrivacyOperation.Client;
using Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Implementations;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.Security;
using Microsoft.PrivacyServices.UX.Monitoring.Events;

namespace Microsoft.PrivacyServices.UX.Core.PxsClient
{
    public static class PxsExtensionForServiceCollection
    {
        /// <summary>
        /// Registers PXS client dependency.
        /// </summary>
        /// <param name="services">Services collection.</param>
        public static IServiceCollection AddPxsClient(this IServiceCollection services)
        {
            services.AddSingleton(ParallaxConfig.Get<IPxsClientConfig>("PxsClient.ini", "IPxsClientConfig"));

            services.AddSingleton<IPrivacyOperationClient>(sp =>
            {
                var requestContextAccessor = sp.GetRequiredService<IInstrumentedRequestContextAccessor>();
                var config = sp.GetRequiredService<IPxsClientConfig>();
                var aadConfig = sp.GetRequiredService<IAzureADConfig>();
                var certificateFinder = sp.GetRequiredService<ICertificateFinder>();
                
                var authClient = new PrivacyOperationAuthClient(
                    certificateFinder.FindBySubjectName(aadConfig.CertSubjectName),
                    aadConfig.AppId,                    
                    config.ResourceId);

                var monitoringHandler = new MonitoringDelegatingHandler<OutgoingServiceEvent>("PXS", requestContextAccessor.GetInstrumentedRequestContext)
                {
                    //  PXS client adds correlation vector from request context.
                    AddCorrelationVectorHeader = false
                };

                var delegateServiceClient = new OSGS.HttpClientCommon.HttpClient(
                    new WebRequestHandler(),
                    new DelegatingHandler[]
                    {
                        monitoringHandler
                    })
                {
                    BaseAddress = new Uri(config.ApiEndpoint)
                };

                return new PrivacyOperationClient(delegateServiceClient, authClient);
            });

            services.AddTransient<IPxsClientProvider, PxsClientProvider>();
            services.AddScoped<IClientProviderAccessor<IPxsClientProvider>, ClientProviderAccessor<IPxsClientProvider, MockPxsClientProvider>>();

            return services;
        }
    }
}
