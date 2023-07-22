using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.PrivacyServices.UX.Startup
{
    public sealed partial class I9nStartup : StartupBase
    {
        public I9nStartup(IHostingEnvironment env) : base(env)
        {
            //  We need to override the "DATADIR" path value in order to offset the value being set within
            //  Product/Source/Microsoft.Osgs.Infra.Platform/AutopilotRuntime.cs.
            Environment.SetEnvironmentVariable("DATADIR", string.Empty);
        }

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            // Custom configuration
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "i9nMode", bool.TrueString }
                });
            services.AddSingleton<IConfiguration>(configBuilder.Build());
        }
    }
}
