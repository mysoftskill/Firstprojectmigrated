namespace Microsoft.PrivacyServices.CommandFeed.Validator
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    /// <summary>
    /// Defines the contract to validate the PrivacyCommand.
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Validates the PrivacyCommand and throws if invalid
        /// </summary>
        /// <param name="verifier">The verifier string</param>
        /// <param name="commandClaims">Claims from the PrivacyCommand that need to be validated</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <exception cref="ArgumentException">Thrown when the verifier is missing or the commandClaims is missing or has no subject</exception>
        /// <exception cref="InvalidPrivacyCommandException">Thrown when the verifier is invalid or the commandClaims are invalid</exception>
        /// <exception cref="KeyDiscoveryException">Thrown when a valid certificate corresponding to the key id was not found</exception>
        /// <exception cref="OperationCanceledException">Thrown when the task is canceled before reaching a valid result</exception>
        Task EnsureValidAsync(string verifier, CommandClaims commandClaims, CancellationToken cancellationToken);

        /// <summary>
        /// List of supported sovereign cloud configurations
        /// </summary>
        List<KeyDiscoveryConfiguration> SovereignCloudConfigurations { get; set; }
    }
}
