using System;
using System.Threading.Tasks;
using Microsoft.Azure.ComplianceServices.Common;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.Flighting;
using Microsoft.PrivacyServices.UX.Core.Security;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FlightingExtensionForServiceCollection
    {
        /// <summary>
        /// Registers flighting dependencies.
        /// </summary>
        /// <param name="services">Services collection.</param>
        public static IServiceCollection AddFlighting(this IServiceCollection services)
        {
            services.AddSingleton(ParallaxConfig.Get<IFlightingConfig>("Flighting.ini", "IFlightingConfig"));

            services.AddSingleton(sp =>
            {
                var flightingConfig = sp.GetRequiredService<IFlightingConfig>();

                string labelName = flightingConfig.Environment == "INT" ? LabelNames.INT:
                flightingConfig.Environment == "CI" ? labelName = LabelNames.CI:
                flightingConfig.Environment == "PPE" ? labelName = LabelNames.PPE: LabelNames.None;

                if (flightingConfig.Environment == "INT")
                {
                    IAppConfiguration appConfiguration = new AppConfiguration(@"local.settings.json");
                    return appConfiguration;
                }
                else
                {
                    IAppConfiguration appConfiguration = new AppConfiguration(new Uri(flightingConfig.ApiEndpoint), labelName);
                    return appConfiguration;
                }
            });

            services.AddScoped<IGroundControl, GroundControl>();

            services.AddTransient<IGroundControlProvider, GroundControlProvider>();
            services.AddScoped<IClientProviderAccessor<IGroundControlProvider>, ClientProviderAccessor<IGroundControlProvider, MockGroundControlProvider>>();

            return services;
        }
    }
}
