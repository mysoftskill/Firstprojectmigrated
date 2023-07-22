// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Diagnostics;

    using global::Owin;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Owin.Hosting;

    /// <summary>
    ///     A host startup decorator which starts the web application listening on a secure (HTTPS) endpoint.
    /// </summary>
    public class HttpsEndpointDecorator : HostDecorator
    {
        private const string SecureAddressPort = "443"; // use the default port for HTTPS

        private readonly string addressPort;

        private readonly Action<IAppBuilder> appBuilder;

        public HttpsEndpointDecorator(Action<IAppBuilder> appBuilder, string addressPort = SecureAddressPort)
        {
            this.addressPort = addressPort;
            this.appBuilder = appBuilder;
        }

        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("HttpsEndpointDecorator executing");

            string secureBaseAddress = "https://{0}:{1}/".FormatInvariant(OwinConstants.IPv4Wildcard, this.addressPort);
            Trace.TraceInformation("Starting service on secure endpoint {0}", secureBaseAddress);
            using (WebApp.Start(secureBaseAddress, this.appBuilder))
            {
                // Activating OWIN adds its own trace listener which outputs to Console. Handle explicity in logging decorator.
                Trace.Listeners.Remove("HostingTraceListener");

                return base.Execute();
            }
        }
    }
}
