namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <summary>
    /// Creates an instance of the <see cref="IKeyDiscoveryService" />.
    /// </summary>
    public interface IKeyDiscoveryServiceFactory
    {
        /// <summary>
        /// Gets <see cref="IKeyDiscoveryService" /> implementation based on the <see cref="EnvironmentConfiguration" />.
        /// </summary>
        /// <param name="subject">Privacy Command Subject</param>
        /// <param name="configuration">Environment configuration</param>
        /// <param name="cloudInstance">cloudInstance if AAD</param>
        /// <param name="loggableInformation">Loggable to provide information in exceptions</param>
        /// <param name="customCache">The custom cache</param>
        /// <returns>IKeyDiscoveryService implementation</returns>
        /// <exception cref="InvalidPrivacyCommandException">Thrown when the subject is invalid</exception>
        IKeyDiscoveryService GetKeyDiscoveryService(
            IPrivacySubject subject, 
            EnvironmentConfiguration configuration,
            string cloudInstance,
            LoggableInformation loggableInformation,
            ICache customCache = null);
    }
}
