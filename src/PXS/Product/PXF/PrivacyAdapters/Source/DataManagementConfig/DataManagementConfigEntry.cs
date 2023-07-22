// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementConfig
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     DataManagementConfig
    /// </summary>
    public class DataManagementConfigEntry : IPxfPartnerConfiguration
    {
        /// <summary>
        ///     Gets or sets the AAD resourceId
        /// </summary>
        public string AadTokenResourceId { get; set; }

        /// <summary>
        ///     Gets or sets the AAD PoP token security scope
        /// </summary>
        public string AadTokenScope { get; set; }

        /// <summary>
        ///     Gets or sets the name of the agent friendly.
        /// </summary>
        public string AgentFriendlyName { get; set; }

        /// <summary>
        ///     Gets or sets the authentication type for the adapter.
        /// </summary>
        public AuthenticationType AuthenticationType { get; set; }

        /// <summary>
        ///     Gets or sets the base URL.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        ///     Gets or sets the name of the counter category.
        /// </summary>
        public string CounterCategoryName { get; set; }

        /// <summary>
        ///     Gets or sets the custom headers.
        /// </summary>
        public IDictionary<string, string> CustomHeaders { get; set; }

        /// <summary>
        ///     Gets or sets the facet domain.
        /// </summary>
        public FacetDomain FacetDomain { get; set; }

        /// <summary>
        ///     Gets or sets the identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     Gets or sets the location category.
        /// </summary>
        public PxfLocationCategory LocationCategory { get; set; }

        /// <summary>
        ///     Gets or sets the msa s2s target site.
        /// </summary>
        public string MsaS2STargetSite { get; set; }

        /// <summary>
        ///     Gets or sets the partner identifier.
        /// </summary>
        public string PartnerId { get; set; }

        /// <summary>
        ///     Gets or sets the partner signature certificate configuration.
        /// </summary>
        public ICertificateConfiguration PartnerSignatureCertificateConfiguration { get; set; }

        /// <summary>
        ///     Gets or sets the PXF adapter version.
        /// </summary>
        public AdapterVersion PxfAdapterVersion { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [real time delete].
        /// </summary>
        public bool RealTimeDelete { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [real time view].
        /// </summary>
        public bool RealTimeView { get; set; }

        /// <summary>
        ///     Gets or sets the retry strategy configuration.
        /// </summary>
        public IRetryStrategyConfiguration RetryStrategyConfiguration { get; set; }

        /// <summary>
        ///     Gets or sets the service point configuration.
        /// </summary>
        public IServicePointConfiguration ServicePointConfiguration { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [skip server cert validation].
        /// </summary>
        public bool SkipServerCertValidation { get; set; }

        /// <summary>
        ///     Gets or sets the supported resources.
        /// </summary>
        public IList<string> SupportedResources { get; set; }

        /// <summary>
        ///     Gets or sets the timeout in milliseconds.
        /// </summary>
        public int TimeoutInMilliseconds { get; set; }

        /// <summary>
        ///     Gets additional parameters that are being passed to the partner adapter.
        /// </summary>
        public IDictionary<string, string> AdditionalParameters { get; set; }

        /// <summary>
        ///     Cortana Profile Reset URl
        /// </summary>
        public string ResetUrl { get; set; }
    }
}
