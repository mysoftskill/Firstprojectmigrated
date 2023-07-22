namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Mail;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;

    using global::Owin;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.IdentityModel.S2S.Configuration;
    using Microsoft.IdentityModel.S2S.Extensions.Owin;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Owin.Logging;
    using Microsoft.Owin.Security.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication.Logging;
    using Microsoft.Identity.Web;
    using Microsoft.IdentityModel.S2S;
    using Microsoft.IdentityModel.S2S.Logging;

    /// <summary>
    /// Enables AAD based authentication.
    /// </summary>
    public class AzureActiveDirectoryProvider : IAuthenticationProvider
    {
        private readonly IAzureActiveDirectoryProviderConfig configuration;
        private readonly IEventWriterFactory eventWriterFactory;
        private readonly IList<X509Certificate2> tokenDecryptionCertificates;
        private IAppConfiguration appConfiguration;

        private readonly string componentName = nameof(AzureActiveDirectoryProvider);
        private static class AzureActiveDirectoryProviderConstants
        {
            public const string OID = "oid";
            public const string TID = "tid";
            public const string SUB = "sub";
            public const string AUTH_SCHEME = "Bearer";
            public const string APP_ID = "appid";
            public const string UPN = "upn";
            public const string ISSUER = "iss";
            public const string IDP = "idp";
            public const string OID_URL = "http://schemas.microsoft.com/identity/claims/objectidentifier";
            public const string TID_URL = "http://schemas.microsoft.com/identity/claims/tenantid";
            public const string NAME_IDENTIFIER_URL = "http://schemas.microsoft.com/identity/claims/nameidentifier";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryProvider" /> class.
        /// </summary>
        /// <param name="configuration">Configuration values for this component.</param>
        /// <param name="tokenDecryptionCertificates">List of certificates to use as token decryption keys.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        /// <param name="appConfiguration">Azure app configuration instance</param>
        public AzureActiveDirectoryProvider(
            IAzureActiveDirectoryProviderConfig configuration,
            IList<X509Certificate2> tokenDecryptionCertificates,
            IEventWriterFactory eventWriterFactory, IAppConfiguration appConfiguration = null)
        {
            this.configuration = configuration;
            this.eventWriterFactory = eventWriterFactory;
            this.tokenDecryptionCertificates = tokenDecryptionCertificates;
            this.appConfiguration = appConfiguration;
        }

        /// <summary>
        /// Gets a value indicating whether or not this provider is enabled.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return this.configuration.Enabled;
            }
        }

        /// <summary>
        /// Register the authentication provider with OWIN.
        /// </summary>
        /// <param name="app">The app builder.</param>
        [ExcludeFromCodeCoverage]
        public void ConfigureAuth(IAppBuilder app)
        {
            if (appConfiguration != null)
            {
                if (appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PDMS.MiseAuthEnabled).Result)
                {
                    this.AuthenticateWithMise(app);
                }
                else
                {
                    this.AuthenticateUsingAAD(app);
                }
            }
            else
            {
                this.AuthenticateUsingAAD(app);
            }
        }

        /// <summary>
        /// New Authentication Flow using MISE and SAL integration
        /// </summary>
        /// <param name="app"> The application object </param>
        private void AuthenticateWithMise(IAppBuilder app)
        {
            var customMISELogger = MiseLogger.Instance;

            var aadAuthenticationOptions = new AadAuthenticationOptions
            {
                Instance = this.configuration.Instance,
                TenantId = this.configuration.Tenant,
                ClientId = this.configuration.ClientId,

                InboundPolicies = this.GetInboundPolicies()
            };
            var s2sAuthenticationOptions = new S2SAuthenticationOptions(aadAuthenticationOptions);
            s2sAuthenticationOptions.Notifications.AuthenticationRequestValidated = async context =>
            {
                var ticket = context.S2SAuthenticationResult.Ticket;

                // IMPORTANT:
                // If your application is a multi-tenant web API that accepts app-tokens, it could receive an app token
                // from any app in any tenant if that app does not have a service principal in the tenant where your web API is running.
                // Unless you have a scenario where you are dependent on authentication without client service principal (deprecated behavior),
                // and provide another kind of authorization, you need to check that the `oid` claim is present. If it's not present, this indicates
                // that there is no service principal for that client in the right tenant (described by the `tid` claim),
                // and in that case, it should not be authorized.
                // Lack of service principal for an app in a tenant indicates that the app was never explicitly installed or consented to use in that tenant.
                var appIdentity = ticket.ApplicationIdentity ?? ticket.SubjectIdentity;
                if (appIdentity != null)
                {
                    // Check that there is an oid claim  in the presented Access token
                    string oid = appIdentity.Claims.FirstOrDefault(x => x.Type == AzureActiveDirectoryProviderConstants.OID
                                        || x.Type == AzureActiveDirectoryProviderConstants.OID_URL)?.Value;
                    if (oid == null)
                    {
                        string tid = appIdentity.Claims.FirstOrDefault(x => x.Type == AzureActiveDirectoryProviderConstants.TID
                                        || x.Type == AzureActiveDirectoryProviderConstants.TID_URL)?.Value;
                        string sub = appIdentity.Claims.FirstOrDefault(x => x.Type == AzureActiveDirectoryProviderConstants.SUB
                                    || x.Type == AzureActiveDirectoryProviderConstants.NAME_IDENTIFIER_URL)?.Value;

                        S2SLogger.LogWarning($"The client '{sub}' calling the web API doesn't have a service principal in tenant '{tid}'.");
                        // and fail the authentication.
                        context.AuthenticationTicket = null;
                        context.SkipToNextMiddleware();
                        return;
                    }
                }

                ClaimsPrincipal claims = new ClaimsPrincipal(ticket.SubjectIdentity ?? ticket.ApplicationIdentity);
            };

            // SAL logs errors for failing policies during request validation. When multiple policies are defined this can cause unwanted error messages in the logs.
            var jwtAuthenticationHandler = s2sAuthenticationOptions.AuthenticationManager.AuthenticationHandlers.First() as JwtAuthenticationHandler;
            jwtAuthenticationHandler.InboundPolicies.First().TokenValidationParameters.LogValidationExceptions = false;
            app.SetLoggerFactory(LoggerFactory.Default);

            // Add SAL and MISE without modules
            app.UseMiseWithDefaultAuthentication(
                s2sAuthenticationOptions,
                configureMise =>
                {
                    return configureMise.ConfigureDefaultModuleCollection(configureModules => { })
                    .WithLogger(customMISELogger);
                }); 
        }

        private ICollection<AadInboundPolicyOptions> GetInboundPolicies()
        {
            var inboundPolicyOption = new AadInboundPolicyOptions
            {
                Label = "PDMSInboundPolicy",
                AuthenticationSchemes = new List<string> { AzureActiveDirectoryProviderConstants.AUTH_SCHEME },
                ValidAudiences = this.configuration.ValidAudiences,
                TokenValidationPolicy = this.GetValidationParametersOptions(),
                AllowMultiTenant = true
            };
            return new List<AadInboundPolicyOptions> { inboundPolicyOption };
        }

        private TokenValidationParametersOptions GetValidationParametersOptions()
        {
            var tokenValidationParametersOptions = new TokenValidationParametersOptions
            {
                ValidateIssuer = this.configuration.EnableIssuerValidation,
                ValidIssuers = this.configuration.ValidIssuers,
                TokenDecryptionCertificates = this.tokenDecryptionCertificates.Select(cert => CertificateDescription.FromCertificate(cert)).ToList(),
            };
            return tokenValidationParametersOptions;
        }

        private void AuthenticateUsingAAD(IAppBuilder app)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = this.configuration.EnableIssuerValidation,
                IssuerValidator = (issuer, token, parameters) =>
                {
                    if (this.configuration.EnableIssuerValidation)
                    {
                        if (this.configuration.ValidIssuers.Contains(issuer))
                        {
                            return issuer;
                        }

                        throw new SecurityTokenInvalidIssuerException("Invalid issuer");
                    }

                    return issuer;
                },
                ValidAudiences = this.configuration.ValidAudiences, // The client must use one of these values (exact match) as its resourceId.
                SaveSigninToken = true
            };

            if (this.configuration.TokenEncryptionEnabled)
            {
                IEnumerable<X509SecurityKey> keys = this.tokenDecryptionCertificates.Select(c => new X509SecurityKey(c));
                tokenValidationParameters.TokenDecryptionKeys = keys;
            }

            app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    Tenant = this.configuration.Tenant,
                    TokenValidationParameters = tokenValidationParameters
                });
        }

        /// <summary>
        /// Retrieve the application id to identify the calling source.
        /// </summary>
        /// <param name="source">The principal information.</param>
        /// <returns>The application id.</returns>
        public string GetApplicationId(IPrincipal source)
        {
            if (source is ClaimsPrincipal claimsPrincipal)
            {
                return claimsPrincipal.FindFirst(AzureActiveDirectoryProviderConstants.APP_ID)?.Value;
            }

            return null;
        }

        /// <summary>
        /// Given a parsed token, copy the values to the principal object.
        /// </summary>
        /// <param name="source">The parse token.</param>
        /// <param name="destination">The object whose values should be set.</param>
        public void SetPrincipal(IPrincipal source, AuthenticatedPrincipal destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination), "Destination value should be created before calling this function.");
            }

            var claimsPrincipal = source as ClaimsPrincipal;
            var tid = claimsPrincipal?.FindFirst(x => x.Type == AzureActiveDirectoryProviderConstants.TID
                                        || x.Type == AzureActiveDirectoryProviderConstants.TID_URL)?.Value;
            var oid = claimsPrincipal?.FindFirst(x => x.Type == AzureActiveDirectoryProviderConstants.OID
                                        || x.Type == AzureActiveDirectoryProviderConstants.OID_URL)?.Value;
            // Only set properties if the authentication method is AAD.
            if (claimsPrincipal != null &&
                !string.IsNullOrEmpty(tid) &&
                !this.IsGuestUser(claimsPrincipal) && IsValidIssuer(claimsPrincipal))
            {
                var appId = this.GetApplicationId(source);

                destination.ClaimsPrincipal = claimsPrincipal;
                destination.ApplicationId = appId;

                destination.UserAlias = claimsPrincipal.FindFirst(x => x.Type == ClaimTypes.Upn || x.Type == AzureActiveDirectoryProviderConstants.UPN)?.Value;

                // The user alias should be the portion of the email before @microsoft.com.
                if (destination.UserAlias != null && destination.UserAlias.Contains("@"))
                {
                    var mail = new MailAddress(destination.UserAlias);
                    destination.UserAlias = mail.User;
                }

                if (claimsPrincipal.FindFirst(x => x.Type == ClaimTypes.Upn || x.Type == AzureActiveDirectoryProviderConstants.UPN) != null)
                {
                    destination.UserId = oid == null ? null : "a:" + oid;
                }
                else if (this.configuration.EnableIntegrationTestOverrides)
                {
                    if (!string.IsNullOrEmpty(oid)) // the `tid` claim doesn’t provide any information about whether the app was installed
                                                    // or consented to use (via a Service Principal creation) in that tenant.
                                                    // The ‘oid’ claim indicates that the app has a Service Principal in the tenant.
                                                    // Multi-tenant applications may be vulnerable to unauthorized cross-tenant access if there are misconfigurations in the authorization logic. 
                                                    // For example, if the application uses the ‘tid’ claim to determine whether the user is authorized to access the tenant,
                                                    // an attacker can create a new tenant and register a new application with the same client id as the victim’s application.
                                                    // The victim’s application will then grant access to the attacker’s application because the ‘tid’ claim is the same.
                                                    // The ‘oid’ claim can be used to mitigate this vulnerability because the attacker’s application will have a different ‘oid’ claim.
                                                    // The ‘oid’ claim is also useful for applications that need to identify the user across multiple tenants.
                                                    // TODO: In future, We should only allow those `oid` values that are pre-authorized or has a app role assignment.
                    {
                        // Set various authentication test overrides.
                        destination.UserId = this.configuration.IntegrationTestUserName;
                        destination.UserAlias = this.configuration.IntegrationTestUserName;
                    }
                    else { destination.UserId = null; }
                }
                else
                {
                    destination.UserId = null;
                }
            }
        }

        /// <summary>
        /// Validates the issuer claim with valid ones
        /// </summary>
        /// <param name="source">The principal information.</param>
        /// <returns>True if its a valid issuer else false.</returns>
        private bool IsValidIssuer(IPrincipal source)
        {
            var claimsPrincipal = source as ClaimsPrincipal;

            if (claimsPrincipal != null)
            {
                var issuer = claimsPrincipal.FindFirst(AzureActiveDirectoryProviderConstants.ISSUER)?.Value ?? string.Empty;

                return this.configuration.ValidIssuers.Contains(issuer);
            }

            return false;
        }

        private bool IsGuestUser(ClaimsPrincipal claimsPrincipal)
        {
            // These will be different if the user is a guest.
            // Otherwise, IDP may not be present at all.
            var idp = claimsPrincipal.FindFirst(AzureActiveDirectoryProviderConstants.IDP)?.Value;
            var iss = claimsPrincipal.FindFirst(AzureActiveDirectoryProviderConstants.ISSUER)?.Value;

            return idp != null && idp != iss;
        }
    }
}
