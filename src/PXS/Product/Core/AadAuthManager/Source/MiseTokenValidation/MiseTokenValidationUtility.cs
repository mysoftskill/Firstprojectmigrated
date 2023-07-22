// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using Microsoft.Identity.ServiceEssentials;
    using Microsoft.IdentityModel.S2S.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;

    public class MiseTokenValidationUtility : IMiseTokenValidationUtility
    {

        public async Task<ClaimsPrincipal> AuthenticateAsync(string authorizationHeaderContent, CancellationToken cancellationToken = default)
        {
            ClaimsPrincipal claims = null;

            if (!string.IsNullOrEmpty(authorizationHeaderContent) && authorizationHeaderContent.Contains("Bearer"))
            {
                // Obtain http request data from your stack
                var httpRequestData = new HttpRequestData();
                httpRequestData.Headers.Add("Authorization", authorizationHeaderContent);

                /*** 1. create mise http context object (for each request) ***/
                var context = new MiseHttpContext(httpRequestData);

                /*** 2. execute mise (for each request) ***/
                var miseResult = await miseHost.HandleAsync(context, cancellationToken).ConfigureAwait(false);

                /*** 3. examine results (for each request) ***/
                if (miseResult.Succeeded)
                {
                    // IMPORTANT:
                    // If your application is a multi-tenant web API that accepts app-tokens, it could receive an app token
                    // from any app in any tenant if that app does not have a service principal in the tenant where your web API is running.
                    // Unless you have a scenario where you are dependent on authentication without client service principal (deprecated behavior),
                    // and provide another kind of authorization, you need to check that the `oid` claim is present. If it's not present, this indicates
                    // that there is no service principal for that client in the right tenant (described by the `tid` claim),
                    // and in that case, it should not be authorized.
                    // Lack of service principal for an app in a tenant indicates that the app was never explicitly installed or consented to use in that tenant.
                    var appIdentity = miseResult.AuthenticationTicket.ActorIdentity ?? miseResult.AuthenticationTicket.SubjectIdentity;

                    // Check that there is an oid claim  in the presented Access token
                    string oid = appIdentity.Claims.FirstOrDefault(x => x.Type == "oid"
                                        || x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                    if (oid == null)
                    {
                        string tid = appIdentity.Claims.FirstOrDefault(x => x.Type == "tid"
                                        || x.Type == "http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
                        string sub = appIdentity.Claims.FirstOrDefault(x => x.Type == "sub"
                                    || x.Type == "http://schemas.microsoft.com/identity/claims/nameidentifier")?.Value;

                        logger.Log($"The client '{sub}' calling the web API doesn't have a service principal in tenant '{tid}'. Authentication Failed.", LogSeverityLevel.Error);
                        // and fail the authentication.
                        return null;
                    }
                    claims = new ClaimsPrincipal(miseResult.AuthenticationTicket.SubjectIdentity ?? miseResult.AuthenticationTicket.ActorIdentity);

                }
                else
                {
                    logger.Log($"Request validation failed.", LogSeverityLevel.Error);

                    /*** 3.2 examine failure, and/or http response produced by a module that failed to handle the request ***/
                    var moduleCreatedFailureResponse = miseResult.MiseContext.ModuleFailureResponse;
                    logger.Log($"HTTP status code: {moduleCreatedFailureResponse?.StatusCode}", LogSeverityLevel.Error);
                }
            }
            return claims;
        }

        public MiseTokenValidationUtility(ILogger privacyServiceLogger, IPrivacyConfigurationManager privacyConfigurationManager)
        {
            var authenticationOptions = MiseAuthenticationConfiguration.GetAuthenticationOptionsInstance(privacyServiceLogger, privacyConfigurationManager);
            var s2sAuthenticationManager = S2SAuthenticationManagerFactory.Default.BuildS2SAuthenticationManager(authenticationOptions);
            this.logger = new MiseLoggerAdapter(privacyServiceLogger);
            miseHost = MiseBuilder.Create(new ApplicationInformationContainer(authenticationOptions.ClientId))
                .WithDefaultAuthentication(s2sAuthenticationManager)
                .ConfigureDefaultModuleCollection(builder =>
                { })
                .WithLogger(logger)
                .Build();
        }

        private static MiseHost<MiseHttpContext> miseHost;

        private readonly MiseLoggerAdapter logger;
    }
}
