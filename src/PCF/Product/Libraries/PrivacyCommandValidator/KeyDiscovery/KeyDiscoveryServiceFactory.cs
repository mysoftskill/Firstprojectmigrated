namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using System.Collections.Concurrent;
    
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <inheritdoc />
    public class KeyDiscoveryServiceFactory : IKeyDiscoveryServiceFactory
    {
        private readonly object msaLock = new object();

        private ICache cache;

        private readonly ConcurrentDictionary<string, IKeyDiscoveryService> aadKeyDiscoveryServices = new ConcurrentDictionary<string, IKeyDiscoveryService>();

        private IKeyDiscoveryService msaKeyDiscoveryService;

        /// <inheritdoc />
        public IKeyDiscoveryService GetKeyDiscoveryService(
            IPrivacySubject subject,
            EnvironmentConfiguration configuration,
            string cloudInstance,
            LoggableInformation loggableInformation,
            ICache customCache = null)
        {
            this.cache = customCache ?? new InMemoryCache();

            // Demographic/Alternate subject is always valid and gets filtered out before it gets here.
            if (subject.GetType() == typeof(AadSubject) || subject.GetType() == typeof(AadSubject2))
            {
                if (string.IsNullOrWhiteSpace(cloudInstance))
                {
                    cloudInstance = CloudInstance.Public;
                }

                if (!this.aadKeyDiscoveryServices.TryGetValue(cloudInstance, out var _))
                {
                    this.aadKeyDiscoveryServices[cloudInstance] = new KeyDiscoveryService(configuration, this.cache);
                }

                return this.aadKeyDiscoveryServices[cloudInstance];
            }

            if (subject.GetType() == typeof(MsaSubject) || subject.GetType() == typeof(DeviceSubject))
            {
                if (this.msaKeyDiscoveryService == null)
                {
                    lock (this.msaLock)
                    {
                        if (this.msaKeyDiscoveryService == null)
                        {
                            return this.msaKeyDiscoveryService = new KeyDiscoveryService(configuration, this.cache);
                        }
                    }
                }

                return this.msaKeyDiscoveryService;
            }

            throw new InvalidPrivacyCommandException("Invalid PrivacyCommand", loggableInformation);
        }
    }
}
