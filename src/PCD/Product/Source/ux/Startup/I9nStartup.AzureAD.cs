using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;

namespace Microsoft.PrivacyServices.UX.Startup
{
    public sealed partial class I9nStartup
    {
        protected override void ConfigurePolicies(AuthorizationOptions options)
        {
            options.AddPolicy("Api", policy =>
            {
                // Api policy is a no-op for i9n mode. 
                policy.RequireAssertion(context => true);
            });

            //  TODO: Figure out a way to configure these policies based on the scenarios.
            //  Per Alex: The code of lambdas that you're passing as arguments to RequireAssertion() are executed in request context.
            //  Then you'll need to create your own "i9n auth scheme" by extending AuthenticationHandler, like JWT auth does.
            //  This will allow you access actual HTTP request and stuff identity claims, which RequireAssertion can analyze here.
            options.AddPolicy("ManualRequests", policy =>
            {
                policy.RequireAssertion(context => true);
            });

            options.AddPolicy("VariantAdmin", policy =>
            {
                policy.RequireAssertion(context => true);
            });

            options.AddPolicy("IncidentManager", policy =>
            {
                policy.RequireAssertion(context => true);
            });
        }

        protected override void UseAuthenticationServices(IApplicationBuilder app)
        {
            // We don't need AzureAD authentication middleware for i9n mode.
        }
    }
}
