// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;

    /// <inheritdoc />
    /// <summary>
    ///     Helper class for authenticating with Aad for Event Hub
    /// </summary>
    public class AadAuthenticationHelper : IAadAuthenticationHelper
    {
        private readonly string appId;

        private readonly Lazy<X509Certificate2> cert;

        private readonly ITokenManager tokenManager = new InstrumentedAadTokenManager(new AadTokenManager());

        public AadAuthenticationHelper(IPrivacyConfigurationManager configMan, ICertificateProvider certProvider)
        {
            this.appId = configMan.AadTokenAuthGeneratorConfiguration.AadAppId;
            this.cert = new Lazy<X509Certificate2>(
                () => certProvider.GetClientCertificate(configMan.AadTokenAuthGeneratorConfiguration.RequestSigningCertificateConfiguration.Subject));
        }

        /// <inheritdoc />
        public Task<string> GetAccessTokenAsync(string authority, string resource, string scope = null)
        {
            return this.tokenManager.GetAppTokenAsync(authority, this.appId, resource, this.cert.Value, cacheable: true, null);
        }
    }
}
