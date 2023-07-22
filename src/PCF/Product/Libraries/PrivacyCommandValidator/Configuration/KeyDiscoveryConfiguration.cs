namespace Microsoft.PrivacyServices.CommandFeed.Validator.Configuration
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Key discovery configuration for sovereign clouds
    /// </summary>
    public class KeyDiscoveryConfiguration
    {
        /// <summary>
        /// The friendly name of the issuers
        /// This should be a subset of string values published/supported by PCF
        /// </summary>
        public List<string> CloudInstances { get; }

        /// <summary>
        /// The issuer Uri, which is used to validate the iss claim in the JWt token 
        /// This is https://aadrvs.msidentity.com/ for the public could
        /// </summary>
        public Uri Issuer { get; }

        /// <summary>
        /// The key discovery endpoint of the AAD RVS
        /// </summary>
        public Uri KeyDiscoveryEndPoint { get; }

        /// <summary>
        /// Does this support Certificate Chain Validation?
        /// </summary>
        public bool IsCertificateChainValidationEnabled { get; }

        /// <summary>
        /// Initializes a <see cref="KeyDiscoveryConfiguration"/>
        /// </summary>
        /// <param name="cloudInstances">Cloud instances that are supported by this AAD RVS endpoint</param>
        /// <param name="issuer">Issuer</param>
        /// <param name="keyDiscoveryEndPoint">AAD RVS endpoint</param>
        /// <param name="isCertificateChainValidationEnabled">Is cert chain validation enabled?</param>
        public KeyDiscoveryConfiguration(List<string> cloudInstances, Uri issuer, Uri keyDiscoveryEndPoint, bool isCertificateChainValidationEnabled)
        {
            this.CloudInstances = cloudInstances;
            this.Issuer = issuer;
            this.KeyDiscoveryEndPoint = keyDiscoveryEndPoint;
            this.IsCertificateChainValidationEnabled = isCertificateChainValidationEnabled;
        }
    }
}
