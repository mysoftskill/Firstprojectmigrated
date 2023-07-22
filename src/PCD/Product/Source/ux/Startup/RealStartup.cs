using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.PrivacyServices.UX.Startup
{
    public sealed partial class RealStartup : StartupBase
    {
        public RealStartup(IHostingEnvironment env) : base(env)
        {}

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            // Custom configuration
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "i9nMode", bool.FalseString }
                });
            services.AddSingleton<IConfiguration>(configBuilder.Build());
        }
    }
}
