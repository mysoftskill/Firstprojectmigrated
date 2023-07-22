using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;

namespace Microsoft.PrivacyServices.UX.Startup
{
    public sealed partial class RealStartup
    {
        protected override void ConfigurePolicies(AuthorizationOptions options)
        {
            options.AddPolicy("Api", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            });

            options.AddPolicy("ManualRequests", policy =>
            {
                policy.RequireRole("ManualRequests");
            });

            options.AddPolicy("VariantAdmin", policy =>
            {
                policy.RequireRole("VariantAdmin");
            });

            options.AddPolicy("IncidentManager", policy =>
            {
                policy.RequireRole("IncidentManager");
            });
        }

        protected override void AddAuthenticationServices(IServiceCollection services)
        {
            base.AddAuthenticationServices(services);
            //Flattens using envtype, which is generated from hosting environment variable, set in appsettings.json
            var aadConfig = EnvironmentConfiguration.Instance.GetConfiguration<IAzureADConfig>("AzureAD.ini", "IAzureADConfig");

            //  This middleware will use JWT from Authorization header to authenticate the request.
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.Authority = aadConfig.Authority;
                    options.Audience = aadConfig.AppId;
                    options.Events = new JwtBearerEvents()
                    {
                        OnTokenValidated = async context =>
                        {
                            // TODO: Make it possible to use AAD ID instead (needs to be able to set orgId).
                            var userInfo = new UserInfo();
                            userInfo.SetId(UserIdType.InternalAppUserId, context.Principal.Claims.First(x => x.Type == "name").Value);

                            // Apply user information to context.
                            var requestContextAccessor = context.HttpContext.RequestServices.GetRequiredService<IInstrumentedRequestContextAccessor>();
                            requestContextAccessor.GetInstrumentedRequestContext().UserInfo = userInfo;

                            // Set claims for authorization to perform role based operations. 
                            await ConfigureClaimsForRoleBasedAuth(
                                context,
                                context.HttpContext.RequestServices.GetRequiredService<IPdmsClientProvider>(),
                                context.HttpContext.RequestServices.GetRequiredService<IRoleBasedAuthConfig>());
                        }
                    };
                    options.TokenValidationParameters = new IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidateAudience = true,
                        ValidAudience = aadConfig.AppId,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true
                    };
                });
        }

        protected override void UseAuthenticationServices(IApplicationBuilder app)
        {
            var aadConfig = app.ApplicationServices.GetRequiredService<IAzureADConfig>();

            const string AuthorizationHeader = "Authorization";
            var jwtMiddlewareTraceSource = new TraceSource("JwtMiddleware");

            //  Sometimes it's impossible to pass JWT as a header (e.g. file downloads). This middleware
            //  will detect missing Authorization header and will use JWT, if it's present in the form.
            //  This MUST be earlier than authentication middleware in the pipeline.
            app.Use(async (context, next) =>
            {
                try
                {
                    if ((HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method))
                        && string.IsNullOrEmpty(context.Request.Headers[AuthorizationHeader]))
                    {
                        var jwtCandidate = context.Request.Form["jwt"];
                        if (!string.IsNullOrEmpty(jwtCandidate))
                        {
                            //  Make it look like request has Authorization header.
                            context.Request.Headers.Add(AuthorizationHeader, new StringValues($"Bearer {jwtCandidate}"));
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    //  We're getting a lot of probing. Probes may be malformed, which results in a lot of noise
                    //  coming from this middleware. Ignore the failure by swallowing exception. 
                    //  See 13986477 for more details.
                    jwtMiddlewareTraceSource.TraceInformation(ex.ToString());
                }

                await next.Invoke();
            });

            //  Add authentication middleware to the pipeline. MUST be last thing this method does.
            app.UseAuthentication();
        }

        // Get the security groups for authenticated user from PDMS. Then check if user is present in any of the "golden" SGs
        // that authorize the user for doing role based operations.
        private async Task ConfigureClaimsForRoleBasedAuth(
            TokenValidatedContext context, 
            IPdmsClientProvider pdmsClient, 
            IRoleBasedAuthConfig roleBasedAuthConfig)
        {
            var requestContext = pdmsClient.CreateNewRequestContext(((JwtSecurityToken)context.SecurityToken).RawData);

            var userDetails = (await pdmsClient.Instance.Users.ReadAsync(requestContext)).Response;
            var userSecurityGroups = userDetails.SecurityGroups;

            var manualRequestsSGs = roleBasedAuthConfig.ManualRequests.Split(',').Select(s => Guid.Parse(s.Trim()));
            var variantAdminSGs = roleBasedAuthConfig.VariantAdmins.Split(',').Select(s => Guid.Parse(s.Trim()));
            var incidentManagerSGs = roleBasedAuthConfig.IncidentManager.Split(',').Select(s => Guid.Parse(s.Trim()));

            if (userSecurityGroups != null)
            {
                if (userSecurityGroups.Any((group) => manualRequestsSGs.Contains(group)))
                {
                    var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
                    claimsIdentity.AddClaim(new Claim(claimsIdentity.RoleClaimType, "ManualRequests"));
                }
                if (userSecurityGroups.Any((group) => variantAdminSGs.Contains(group)))
                {
                    var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
                    claimsIdentity.AddClaim(new Claim(claimsIdentity.RoleClaimType, "VariantAdmin"));
                }
                if (userSecurityGroups.Any((group) => incidentManagerSGs.Contains(group)))
                {
                    var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
                    claimsIdentity.AddClaim(new Claim(claimsIdentity.RoleClaimType, "IncidentManager"));
                }
            }
        }
    }
}
