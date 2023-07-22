// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     CredentialServiceClient wraps the auto-generated <see cref="CredentialServiceAPISoapServer" /> with an Interface.
    /// </summary>
    public class CredentialServiceClient : ICredentialServiceClient
    {
        private const string UrlSuffix = "/pksecure/PPSACredentialPK.srf";

        private readonly IPrivacyPartnerAdapterConfiguration adapterConfig;

        private readonly X509Certificate2 clientCert;

        private readonly string clientSiteId;

        /// <inheritdoc />
        public Uri TargetUri { get; }

        /// <summary>
        ///     Creates a new instance of the <see cref="CredentialServiceClient" />
        /// </summary>
        /// <param name="adapterConfig">adapter config</param>
        /// <param name="msaSiteConfig">msa site config</param>
        /// <param name="certProvider">cert provider</param>
        public CredentialServiceClient(IPrivacyPartnerAdapterConfiguration adapterConfig, IMsaIdentityServiceConfiguration msaSiteConfig, ICertificateProvider certProvider)
        {
            this.adapterConfig = adapterConfig ?? throw new ArgumentNullException(nameof(adapterConfig));
            this.clientCert = certProvider.GetClientCertificate(msaSiteConfig.CertificateConfiguration);
            this.clientSiteId = msaSiteConfig.ClientId;
            this.TargetUri = new Uri($"{adapterConfig.BaseUrl.TrimEnd('/')}{UrlSuffix}");
        }

        /// <inheritdoc />
        public async Task<string> GetGdprVerifierAsync(
            tagPASSID targetIdentifier,
            eGDPR_VERIFIER_OPERATION operation,
            IDictionary<string, string> additionalClaims,
            string unauthSessionID = "",
            string optionalParams = "",
            string proxyTicket = "",
            bool requestPreverifier = false)
        {
            using (var proxy = new CredentialServiceAPISoapServer { Timeout = this.adapterConfig.TimeoutInMilliseconds })
            {
                proxy.ClientCertificates.Add(this.clientCert);
                proxy.Url = this.TargetUri.AbsoluteUri;

                proxy.WSSecurityHeader = new tagWSSECURITYHEADER
                {
                    version = EnumSHVersion.eshHeader25,
                    ppSoapHeader25 = requestPreverifier ? this.CreateSoapHeaderWithPreverifier(proxyTicket) : this.CreateSoapHeader(proxyTicket),
                    unauthSessionID = string.IsNullOrEmpty(unauthSessionID) ? Guid.NewGuid().ToString("N") : unauthSessionID
                };

                // Note: Access to disposed closure. await on this to prevent dispose from happening before task finishes.
                Task<string> task = Task.Factory.FromAsync(
                    (callback, state) => proxy.BeginGetGdprVerifier(
                        targetIdentifier,
                        operation,
                        additionalClaims?.Keys.ToArray(),
                        additionalClaims?.Values.ToArray(),
                        optionalParams,
                        callback,
                        state),
                    ar =>
                    {
                        string proxyResponse = proxy.EndGetGdprVerifier(ar, out string gdprVerifier);
                        // the proxy response is a guid, unique to the token. But we only need the verifier token.
                        return gdprVerifier;
                    },
                    TaskCreationOptions.None);

                return await this.CallWithCancelAsync(task, () => proxy.Abort());
            }
        }

        /// <inheritdoc />
        public async Task<string> GetSigninNamesAndCidsForNetIdAsync(string puid, string unauthSessionID)
        {
            using (var proxy = new CredentialServiceAPISoapServer { Timeout = this.adapterConfig.TimeoutInMilliseconds })
            {
                proxy.ClientCertificates.Add(this.clientCert);
                proxy.Url = this.TargetUri.AbsoluteUri;
                proxy.WSSecurityHeader = new tagWSSECURITYHEADER
                {
                    version = EnumSHVersion.eshHeader25,
                    ppSoapHeader25 = this.CreateSoapHeader(null),
                    unauthSessionID = string.IsNullOrEmpty(unauthSessionID) ? Guid.NewGuid().ToString("N") : unauthSessionID
                };

                Task<string> task = Task.Factory.FromAsync(
                    (callback, state) => proxy.BeginGetSigninNamesAndCIDsForNetIDs(puid, callback, state),
                    ar =>
                    {
                        string response = proxy.EndGetSigninNamesAndCIDsForNetIDs(ar);

                        return response;
                    },
                    TaskCreationOptions.None);

                return await this.CallWithCancelAsync(task, () => proxy.Abort());
            }
        }

        private string CreateSoapHeader(string userProxyTicket)
        {
            // Format defined @ https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/webframe.html
            // Section: Generating a SOAP Header
            return "<s:ppSoapHeader xmlns:s=\"http://schemas.microsoft.com/Passport/SoapServices/SoapHeader\" version=\"1.0\">"
                   + "<s:credentials>"
                   + $"<s:passportSiteID>{this.clientSiteId}</s:passportSiteID>"
                   + $"<s:binaryCredential issuingAuthority=\"passport.net\" valueType=\"MicrosoftPassportCompact\" encodingType=\"xsd:base64Binary\">{userProxyTicket}</s:binaryCredential>"
                   + "</s:credentials>"
                   + "<s:sitetoken>"
                   + $"<t:siteheader xmlns:t=\"http://schemas.microsoft.com/Passport/SiteToken\" id=\"{this.clientSiteId}\" />"
                   + "</s:sitetoken>"
                   + "</s:ppSoapHeader>";
        }

        private string CreateSoapHeaderWithPreverifier(string userProxyTicket)
        {
            // Format defined @ https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/webframe.html
            // Section: Generating a SOAP Header
            StringBuilder sb = new StringBuilder();
            sb.Append(@"<s:ppSoapHeader xmlns:s=""http://schemas.microsoft.com/Passport/SoapServices/SoapHeader"" version=""1.0"">");

            if (!string.IsNullOrEmpty(userProxyTicket))
            {
                sb.Append(@"<s:credentials>");
                sb.AppendFormat(@"<s:passportSiteID>{0}</s:passportSiteID>", this.clientSiteId);
                sb.AppendFormat(@"<s:binaryCredential issuingAuthority=""passport.net"" valueType=""MicrosoftPassportCompact"" encodingType=""xsd:base64Binary"">{0}</s:binaryCredential>", userProxyTicket);
                sb.Append(@"</s:credentials>");
            }

            sb.Append(@"<s:sitetoken>");
            sb.AppendFormat(@"<t:siteheader xmlns:t=""http://schemas.microsoft.com/Passport/SiteToken"" id=""{0}"" />", this.clientSiteId);
            sb.Append(@"</s:sitetoken>");
            sb.Append(@"</s:ppSoapHeader>");

            return sb.ToString();
        }

        private async Task<T> CallWithCancelAsync<T>(Task<T> task, Action cancelAction)
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Token.Register(cancelAction);
                cts.CancelAfter(this.adapterConfig.TimeoutInMilliseconds);
                
                return await task;
            }
        }
    }
}
