// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    /// <summary>
    /// Address
    /// </summary>
    public sealed class Address
    {
        /// <summary>
        /// Gets or sets the address line1.
        /// </summary>
        public string AddressLine1 { get; set; }

        /// <summary>
        /// Gets or sets the address line2.
        /// </summary>
        public string AddressLine2 { get; set; }

        /// <summary>
        /// Gets or sets the address line3.
        /// </summary>
        public string AddressLine3 { get; set; }

        /// <summary>
        /// Gets or sets the formatted address.
        /// </summary>
        public string FormattedAddress { get; set; }

        /// <summary>
        /// Gets or sets the country/region.
        /// </summary>
        public string CountryRegion { get; set; }

        /// <summary>
        /// Gets or sets the country/region iso2.
        /// </summary>
        public string CountryRegionIso2 { get; set; }

        /// <summary>
        /// Gets or sets the locality.
        /// </summary>
        public string Locality { get; set; }

        /// <summary>
        /// Gets or sets the postal code.
        /// </summary>
        public string PostalCode { get; set; }
    }
}
