namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Provides a message handler in the HTTP stack that sends and receives forked shadow requests.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "lifetime object")]
    [ExcludeFromCodeCoverage]
    internal class StressRequestForwarder
    {
        private const string StressForwardedMessageHeader = "X-Forwarded-From";
        internal const string StressDelegatedAuthHeader = "X-Stress-Delegated-Authentication";

        private static readonly Lazy<StressRequestForwarder> LazyInstance = new Lazy<StressRequestForwarder>(() => new StressRequestForwarder());

        private readonly HttpClient client;

        /// <summary>
        /// Initializes a new instance of the shadow fork handler.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private StressRequestForwarder()
        {
            this.client = new HttpClient(new WebRequestHandler
            {
                ServerCertificateValidationCallback = ValidateServerCert
            });
        }

        public static StressRequestForwarder Instance => LazyInstance.Value;

        /// <summary>
        /// Sends a forwarded request to the stress environment.
        /// </summary>
        public void SendForwardedRequest(
            PcfAuthenticationContext context, 
            HttpRequestMessage request, 
            HttpContent requestContent,
            AgentId agentId = null,
            AssetGroupId assetGroupId = null,
            CommandId commandId = null)
        {
            if (EnvironmentInfo.IsUnitTest || !Config.Instance.StressForwarding.Enabled)
            {
                return;
            }

            if (request.Headers.Contains(StressForwardedMessageHeader))
            {
                // Make sure shadowing enabled and not in a loop.
                return;
            }

            List<ICustomOperatorContext> flightOptions = new List<ICustomOperatorContext>();
            if (agentId != null)
            {
                flightOptions.Add(FlightingContext.FromAgentId(agentId));
            }

            if (assetGroupId != null)
            {
                flightOptions.Add(FlightingContext.FromAssetGroupId(assetGroupId));
            }

            if (commandId != null)
            {
                flightOptions.Add(FlightingContext.FromCommandId(commandId));
            }

            if (FlightingUtilities.IsEnabledAny(FlightingNames.StressForwardingPercentage, flightOptions))
            {
                this.SendForwardedRequestImpl(context, request, requestContent);
            }
        }

        /// <summary>
        /// Forks a request.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void SendForwardedRequestImpl(PcfAuthenticationContext context, HttpRequestMessage request, HttpContent modifiedContent)
        {
            try
            {
                UriBuilder uriBuilder = new UriBuilder(request.RequestUri);
                uriBuilder.Host = Config.Instance.StressForwarding.ForwardingTargetHostName;
                Uri uri = uriBuilder.Uri;

                var servicePoint = ServicePointManager.FindServicePoint(uri);
                servicePoint.ConnectionLeaseTimeout = 25000;
                servicePoint.ConnectionLimit = 100;

                HttpRequestMessage forkedRequest = new HttpRequestMessage(request.Method, uri);
                forkedRequest.Headers.Add(StressForwardedMessageHeader, EnvironmentInfo.NodeName);
                forkedRequest.Headers.Add(StressDelegatedAuthHeader, JsonConvert.SerializeObject(context));

                // Copy over headers.
                foreach (var header in request.Headers)
                {
                    forkedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Prevent CV from being duplicated across datacenters.
                forkedRequest.Headers.Remove("MS-CV");

                if (modifiedContent != null)
                {
                    var length = modifiedContent.Headers.ContentLength;
                    forkedRequest.Content = modifiedContent;

                    foreach (var header in request.Content.Headers)
                    {
                        forkedRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    forkedRequest.Content.Headers.ContentLength = length;
                }

                // accept compressed responses from stress -- less bandwidth.
                forkedRequest.Headers.AcceptEncoding.Clear();
                forkedRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

                StressThreadpool.Instance.EnqueueRequest(() => this.client.SendAsync(forkedRequest, CancellationToken.None));
            }
            catch
            {
                // Swallow failures here -- don't let any stress business affect prod.
            }
        }

        /// <summary>
        /// Performs some cert validation. If .NET was happy with the cert, we return. Otherwise, we check for a known-good thumbprint.
        /// </summary>
        private static bool ValidateServerCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            X509Certificate2 fancyCert = certificate as X509Certificate2;
            if (errors == SslPolicyErrors.None || errors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                if (FlightingUtilities.IsStringValueEnabled(FlightingNames.TrustedStressSSLThumbprints, fancyCert?.Thumbprint?.ToUpperInvariant() ?? string.Empty))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
