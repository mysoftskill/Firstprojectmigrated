using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.UhfClient;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class UhfExtensionForServiceCollection
    {
        public static IServiceCollection AddUhfClient(this IServiceCollection services)
        {
            services.AddSingleton(ParallaxConfig.Get<IUhfClientConfig>("UhfClient.ini", "IUhfClientConfig"));

            services.AddSingleton<IUhfHttpClient, UhfHttpClient>();

            services.AddScoped<IUhfClient>(sp =>
            {
                var config = sp.GetRequiredService<IUhfClientConfig>();
                var httpClient = sp.GetRequiredService<IUhfHttpClient>();
                return new UhfClient(config, httpClient);
            });

            return services;
        }
    }
}
