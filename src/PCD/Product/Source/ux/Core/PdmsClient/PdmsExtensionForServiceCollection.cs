using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.PrivacyServices.UX.Monitoring.Events;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.Security;
using Microsoft.PrivacyServices.DataManagement.Client.AAD;
using Microsoft.Osgs.Infra.Monitoring.Context;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PdmsExtensionForServiceCollection
    {
        /// <summary>
        /// Registers PDMS client dependency.
        /// </summary>
        /// <param name="services">Services collection.</param>
        public static IServiceCollection AddPdmsClient(this IServiceCollection services)
        {
            services.AddSingleton(ParallaxConfig.Get<IPdmsClientConfig>("PdmsClient.ini", "IPdmsClientConfig"));

            services.AddSingleton<PrivacyServices.DataManagement.Client.V2.IDataManagementClient>(sp =>
            {
                var requestContextAccessor = sp.GetRequiredService<IInstrumentedRequestContextAccessor>();
                var config = sp.GetRequiredService<IPdmsClientConfig>();

                var monitoringHandler = new MonitoringDelegatingHandler<OutgoingServiceEvent>("PDMSv2", requestContextAccessor.GetInstrumentedRequestContext)
                {
                    //  PDMS client adds correlation vector from request context.
                    AddCorrelationVectorHeader = false
                };

                var httpClient = HttpClientFactory.Create(monitoringHandler);
                httpClient.BaseAddress = new Uri(config.Endpoint);

                return new PrivacyServices.DataManagement.Client.V2.DataManagementClient(new HttpProxyClient(httpClient));
            });

            services.AddSingleton<IDataOwnerNameCache, DataOwnerNameCache>();
            services.AddSingleton<IVariantNameCache, VariantNameCache>();
            services.AddScoped<IPdmsClientProvider, PdmsClientProvider>(sp =>
            {
                var pdmsClientInstance = sp.GetRequiredService<PrivacyServices.DataManagement.Client.V2.IDataManagementClient>();
                var requestContextAccessor = sp.GetRequiredService<IInstrumentedRequestContextAccessor>();
                var aadConfig = sp.GetRequiredService<IAzureADConfig>();
                var certificateFinder = sp.GetRequiredService<ICertificateFinder>();
                var correlationVectorContext = sp.GetRequiredService<ICorrelationVectorContext>();
                var hostingEnvironment = sp.GetRequiredService<IHostingEnvironment>();
                var bearerTokenAccessor = sp.GetRequiredService<IJwtBearerTokenAccessor>();

                var authenticationProviderFactory =
                    new ServiceAzureActiveDirectoryProviderFactory(aadConfig.AppId, certificateFinder.FindBySubjectName(aadConfig.CertSubjectName), targetProductionEnvironment: hostingEnvironment.IsProduction(), sendX5c: true);

                return new PdmsClientProvider(pdmsClientInstance, correlationVectorContext, bearerTokenAccessor, authenticationProviderFactory);
            });
            services.AddScoped<IClientProviderAccessor<IPdmsClientProvider>, ClientProviderAccessor<IPdmsClientProvider, MockPdmsClientProvider>>();

            return services;
        }

        /// <summary>
        /// Registers privacy policy dependency.
        /// </summary>
        /// <param name="services">Services collection.</param>
        public static IServiceCollection AddPrivacyPolicies(this IServiceCollection services)
        {
            //  Pre-create and load data by accessing the instance. Do not remove trace line - it's there to ensure call to .Get() is not optimized away.
            var privacyPolicyAccessor = new PrivacyPolicyAccessor();
            var policy = privacyPolicyAccessor.Get();
            System.Diagnostics.Trace.WriteLine($"Privacy policy loaded: {policy.DataTypes.Set.Count()} data types; {policy.Capabilities.Set.Count()} capabilities; {policy.Protocols.Set.Count()} protocols.");

            return services.AddSingleton<IPrivacyPolicyAccessor>(privacyPolicyAccessor);
        }
    }
}
