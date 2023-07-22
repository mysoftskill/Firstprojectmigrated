// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;

    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Implementations;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Interfaces;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.OSGS.HttpClientCommon;

    /// <summary>
    ///     Privacy-Experience Client
    /// </summary>
    public partial class PrivacyExperienceClient
    {
        private const string IfMatchHeader = "If-Match";

        private readonly IPrivacyAuthClient authClient;

        private readonly IHttpClient httpClient;

        private readonly IServicePointManagerConfig servicePointManagerConfig;

        private ServicePoint servicePoint;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacyExperienceClient" /> class.
        /// </summary>
        /// <param name="serviceEndpoint">The service endpoint.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="authClient">The authentication client.</param>
        /// <param name="servicePointManagerConfig">The Service Point Manager config</param>
        public PrivacyExperienceClient(Uri serviceEndpoint, IHttpClient httpClient, IPrivacyAuthClient authClient, IServicePointManagerConfig servicePointManagerConfig = null)
        {
            serviceEndpoint.ThrowOnNull(nameof(serviceEndpoint));
            httpClient.ThrowOnNull(nameof(httpClient));
            authClient.ThrowOnNull(nameof(authClient));

            this.httpClient = httpClient;
            this.authClient = authClient;

            this.httpClient.BaseAddress = serviceEndpoint;
            this.httpClient.MessageHandler.AttachClientCertificate(this.authClient.ClientCertificate);

            var assembly = typeof(PrivacyExperienceClient).Assembly;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string clientVersion = fileVersionInfo.FileVersion;
            this.httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PrivacyExperienceClient", clientVersion));

            this.servicePointManagerConfig = servicePointManagerConfig ?? new DefaultServicePointManagerConfig();
            this.SetPxsServicePointProperties();
        }

        /// <summary>
        ///     Adds Accept header to request.
        /// </summary>
        private HttpRequestMessage AddAcceptJsonHeader(HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return requestMessage;
        }

        /// <summary>
        ///     Sets the SPM properties for the base address
        /// </summary>
        private void SetPxsServicePointProperties()
        {
            // As per MSDN FindServicePoint either uses existing instance or creates new one, so not checking for null
            this.servicePoint = ServicePointManager.FindServicePoint(this.httpClient.BaseAddress);

            this.servicePoint.ConnectionLeaseTimeout = this.servicePointManagerConfig.ConnectionLeaseTimeout;
            this.servicePoint.Expect100Continue = this.servicePointManagerConfig.Expect100Continue;
            this.servicePoint.MaxIdleTime = this.servicePointManagerConfig.MaxIdleTime;
            this.servicePoint.UseNagleAlgorithm = this.servicePointManagerConfig.UseNagleAlgorithm;
        }

        /// <summary>
        ///     Sets <see cref="HttpRequestMessage.Content" /> to <see cref="ObjectContent" /> formatted to JSON.
        /// </summary>
        private HttpRequestMessage SetRequestBody<T>(HttpRequestMessage requestMessage, T data)
        {
            requestMessage.Content = new ObjectContent<T>(data, new JsonMediaTypeFormatter());

            return requestMessage;
        }
    }
}
