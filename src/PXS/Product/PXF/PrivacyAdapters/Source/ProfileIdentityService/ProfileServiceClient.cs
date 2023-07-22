// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;

    /// <summary>
    ///     Client to use to talk with MSA's Profile Service
    /// </summary>
    public class ProfileServiceClient : IProfileServiceClient
    {
        private const string UrlSuffix = "/pksecure/PPSAProfilePK.srf";

        private readonly IPrivacyPartnerAdapterConfiguration adapterConfig;

        private readonly X509Certificate2 clientCert;

        private readonly string clientSiteId;

        /// <summary>
        ///     Gets the target Uri
        /// </summary>
        public Uri TargetUri { get; }

        /// <summary>
        ///     Creates a new instance of the ProfileServiceClient
        /// </summary>
        /// <param name="adapterConfig">The adapter configuration to use</param>
        /// <param name="msaSiteConfig">The MSA site configuration to use</param>
        /// <param name="certProvider">The certificate provider to use</param>
        public ProfileServiceClient(IPrivacyPartnerAdapterConfiguration adapterConfig, IMsaIdentityServiceConfiguration msaSiteConfig, ICertificateProvider certProvider)
        {
            this.adapterConfig = adapterConfig ?? throw new ArgumentNullException(nameof(adapterConfig));
            this.clientCert = certProvider.GetClientCertificate(msaSiteConfig.CertificateConfiguration);
            this.clientSiteId = msaSiteConfig.ClientId;
            this.TargetUri = new Uri($"{adapterConfig.BaseUrl.TrimEnd('/')}{UrlSuffix}");
        }

        /// <summary>
        ///     Gets the profile attributes for the specified account
        /// </summary>
        /// <param name="puid">The Puid for the account to get attributes for</param>
        /// <param name="attributeList">The attributes to grab, "*" for all</param>
        /// <returns>An XML block</returns>
        /// <remarks>
        ///     https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/Profile_GetProfileByAttribute.html
        /// </remarks>
        public async Task<string> GetProfileByAttributesAsync(string puid, string attributeList)
        {
            using (ProfileServiceAPISoapServer proxy = this.GetProxy())
            {
                var internalTask = new TaskCompletionSource<string>();
                proxy.GetProfileByAttributesCompleted += (s, e) =>
                {
                    if (e.Error != null || !string.IsNullOrEmpty(e.pbstrErrorBlob))
                    {
                        internalTask.TrySetException(MakeException(e.Error, e.pbstrErrorBlob));
                    }
                    else if (e.Cancelled)
                    {
                        internalTask.TrySetCanceled();
                    }
                    else
                    {
                        internalTask.TrySetResult(e.Result[0]);
                    }
                };

                // Even though it takes in an array, the API only supports an array of 1.
                proxy.GetProfileByAttributesAsync(new[] { puid }, attributeList, 0); // Last parameter is not implemented, set to 0

                // Perform await to prevent proxy from being disposed before callback is fired
                return await internalTask.Task.ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Creates the SOAP header for making requests
        /// </summary>
        /// <returns>The SOAP header</returns>
        private string CreateSoapHeader()
        {
            // Format defined @ https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/webframe.html
            // Section: Generating a SOAP Header
            return "<s:ppSoapHeader xmlns:s=\"http://schemas.microsoft.com/Passport/SoapServices/SoapHeader\" version=\"1.0\">"
                   + "<s:sitetoken>"
                   + "<t:siteheader xmlns:t=\"http://schemas.microsoft.com/Passport/SiteToken\" id=\""
                   + this.clientSiteId
                   + "\" />"
                   + "</s:sitetoken>"
                   + "</s:ppSoapHeader>";
        }

        /// <summary>
        ///     Gets the proxy to use to talk with MSA
        /// </summary>
        /// <returns>The proxy</returns>
        private ProfileServiceAPISoapServer GetProxy()
        {
            using (var proxy = new ProfileServiceAPISoapServer
            {
                Timeout = this.adapterConfig.TimeoutInMilliseconds,
                Url = this.TargetUri.AbsoluteUri,
                WSSecurityHeader = new tagWSSECURITYHEADER
                {
                    version = EnumSHVersion.eshHeader25,
                    ppSoapHeader25 = this.CreateSoapHeader()
                }
            })
            {
                proxy.ClientCertificates.Add(this.clientCert);

                return proxy;
            }
        }

        /// <summary>
        ///     Creates an exception from an error or error blob
        /// </summary>
        /// <param name="existing">An exception that may have been returned by the API called</param>
        /// <param name="errorBlob">The error blob that was returned from the API call</param>
        /// <returns>An exception</returns>
        private static Exception MakeException(Exception existing, string errorBlob)
        {
            if (existing != null)
            {
                return existing;
            }

            using (var reader = new StringReader(errorBlob))
            {
                var serializer = new XmlSerializer(typeof(MultipleErrors));
                var errors = (MultipleErrors)serializer.Deserialize(reader);
                foreach (MultipleErrorsError item in errors.Items)
                {
                    // Errors come back as HRESULTs in the XML
                    if (int.TryParse(item.HR.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int errorCode))
                    {
                        // GetProfileByAttributes returns the 0xCFFFFC15, 0x8004812f HResults when the user is not found
                        // So for these two errorCodes, make sure the service returns a 404 rather than 500.
                        //   HResult = 0xCFFFFC15: Can not find the record in Database
                        //   HResult = 0x8004812f: The specified profile does not exist.
                        // Check https://errors/ for different errors
                        if (errorCode == unchecked((int)0xCFFFFC15) || errorCode == unchecked((int)0x8004812f))
                        {
                            return new PrivacySubjectInvalidException("The specified profile does not exist.");
                        }
                            
                        return Marshal.GetExceptionForHR(errorCode);
                    }
                }
            }

            // If an HRESULT couldn't be found, just return the blob as an exception
            return new Exception(errorBlob);
        }
    }
}
