using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.Security;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.PrivacyServices.UX.Startup
{
    public abstract partial class StartupBase
    {
        /// <summary>
        /// Configure policies that gate access to controller APIs.
        /// </summary>
        protected abstract void ConfigurePolicies(AuthorizationOptions options);

        /// <summary>
        /// Configures authentication services for application.
        /// </summary>
        protected virtual void AddAuthenticationServices(IServiceCollection services)
        {
            services.AddSingleton(ParallaxConfig.Get<IAzureADConfig>("AzureAD.ini", "IAzureADConfig"));
            services.AddSingleton(ParallaxConfig.Get<IRoleBasedAuthConfig>("RoleBasedAuth.ini", "IRoleBasedAuthConfig"));
            services.AddTransient<IJwtBearerTokenAccessor, JwtBearerTokenAccessor>();
            
            // More: https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies
            services.AddAuthorization(options =>
            {
                ConfigurePolicies(options);
            });
        }

        /// <summary>
        /// Makes application use authentication services.
        /// </summary>
        protected abstract void UseAuthenticationServices(IApplicationBuilder app);
    }
}
