namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter
{
    using System;
    using System.Text;
    using System.Threading.Tasks;

    using global::Azure.Core;
    using global::Azure.Messaging.EventHubs;
    using global::Azure.Messaging.EventHubs.Producer;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;

    /// <summary>
    /// Wrapper to send events into both: the real AnaheimId eventhub and mock environment.
    /// </summary>
    public class AidEventHubProducer : IEventHubProducer
    {
        /// <summary>
        /// Real AID eventhub hosted in AnaheimId Microsoft Tenant.
        /// </summary>
        private readonly EventHubProducer aidEventHubProducer = null;
        
        /// <summary>
        /// App config.
        /// </summary>
        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        /// Creates a new instance of <see cref="AidEventHubProducer" />
        /// </summary>
        /// <param name="config">Config.</param>
        /// <param name="appConfiguration">App config.</param>
        public AidEventHubProducer(IPrivacyConfigurationManager config, IAppConfiguration appConfiguration)
        {
            this.appConfiguration = appConfiguration;

            var nameSpace = config.AdaptersConfiguration.AnaheimIdAdapterConfiguration.EventHubConfiguration.EventHubNamespace;
            var eventHubName = config.AdaptersConfiguration.AnaheimIdAdapterConfiguration.EventHubConfiguration.EventHubName1;
            var fullyQualifiedNamespace = $"{nameSpace}.servicebus.windows.net";
            
            var cert = CertificateFinder.FindCertificateByName(config.AdaptersConfiguration.AnaheimIdAdapterConfiguration.AIdAuthConfiguration.CertSubjectName);

            // Obtain SN/I Client credentials NGP Multitenant Service Principal
            var token = new ConfidentialCredential(
                    config.AdaptersConfiguration.AnaheimIdAdapterConfiguration.AIdAuthConfiguration.TenantId,
                    config.AdaptersConfiguration.AnaheimIdAdapterConfiguration.AIdAuthConfiguration.ClientId,
                    cert);

            this.aidEventHubProducer = new EventHubProducer(fullyQualifiedNamespace, eventHubName, token);
        }

        /// <summary>
        ///     Send a message to EventHub.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>Task</returns>
        public async Task SendAsync(string message)
        {
            await this.aidEventHubProducer.SendAsync(message);
        }
    }
}
