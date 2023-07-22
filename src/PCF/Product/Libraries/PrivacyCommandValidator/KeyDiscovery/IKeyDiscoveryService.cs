namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using System;
    using System.IdentityModel.Tokens;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <summary>
    /// Defines the contract for a service that will fetch the <see cref="X509Certificate2" />.
    /// </summary>
    public interface IKeyDiscoveryService
    {
        /// <summary>
        /// Fetches the X509 certificate corresponding to the keyId asynchronously.
        /// </summary>
        /// <param name="keyId">The key identifier</param>
        /// <param name="loggableInformation">Loggable to provide information in exceptions</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task returning a valid <see cref="X509Certificate2" /> task</returns>
        /// <exception cref="OperationCanceledException">Thrown when the task is canceled before reaching a valid result</exception>
        /// <exception cref="KeyDiscoveryException">Thrown when a valid certificate corresponding to the key id was not found</exception>
        Task<X509Certificate2> GetCertificate(string keyId, LoggableInformation loggableInformation,  CancellationToken cancellationToken);
    }
}
