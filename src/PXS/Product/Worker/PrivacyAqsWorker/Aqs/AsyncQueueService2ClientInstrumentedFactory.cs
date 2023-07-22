// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs
{
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;

    internal class AsyncQueueService2ClientInstrumentedFactory : IAsyncQueueService2ClientFactory
    {
        private IPrivacyConfigurationManager privacyConfig;

        public AsyncQueueService2ClientInstrumentedFactory(IPrivacyConfigurationManager privacyConfig)
        {
            this.privacyConfig = privacyConfig ?? throw new ArgumentNullException(nameof(privacyConfig));
        }

        public IAsyncQueueService2 Create(IAqsConfiguration config, ILogger logger)
        {
            string clientName;
            IAsyncQueueService2 client;
            if (config.UseRestClient)
            {
                clientName = "AQS_PRIVACY_MOCK";
                client = this.CreateRestClient(config);
            }
            else
            {
                clientName = "AQS";
                client = this.CreateWcfClient(config, logger);
            }

            return new AsyncQueueService2ClientInstrumented(client, clientName);
        }

        private IAsyncQueueService2 CreateRestClient(IAqsConfiguration config)
        {
            return new AqsRestClient(this.privacyConfig, new Uri(config.Endpoint));
        }

        private IAsyncQueueService2 CreateWcfClient(IAqsConfiguration config, ILogger logger)
        {
            var binding = new WSHttpBinding(SecurityMode.Transport)
            {
                MaxReceivedMessageSize = int.MaxValue
            };

            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;

            var endpoint = new EndpointAddress(config.Endpoint);
            var aqsClient = new AsyncQueueService2Client(binding, endpoint);
            
            CertificateProvider provider = new CertificateProvider(logger);
            X509Certificate2 cert = provider.GetClientCertificate(config.CertificateConfiguration.Subject, StoreLocation.LocalMachine);
            aqsClient.ClientCredentials.ClientCertificate.Certificate = cert;

            ServicePoint servicePoint = ServicePointManager.FindServicePoint(endpoint.Uri);
            servicePoint.ConnectionLimit = config.ConnectionLimit;

            return aqsClient;
        }
    }
}
