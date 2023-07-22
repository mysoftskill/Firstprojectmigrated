// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using Newtonsoft.Json;

    /// <summary>
    /// Address V1
    /// </summary>
    public sealed class AddressV1
    {
        /// <summary>
        /// Gets or sets the address line1.
        /// </summary>
        [JsonProperty("addressLine1")]
        public string AddressLine1 { get; set; }

        /// <summary>
        /// Gets or sets the address line2.
        /// </summary>
        [JsonProperty("addressLine2")]
        public string AddressLine2 { get; set; }

        /// <summary>
        /// Gets or sets the address line3.
        /// </summary>
        [JsonProperty("addressLine3")]
        public string AddressLine3 { get; set; }

        /// <summary>
        /// Gets or sets the formatted address.
        /// </summary>
        [JsonProperty("formattedAddress")]
        public string FormattedAddress { get; set; }

        /// <summary>
        /// Gets or sets the country/region.
        /// </summary>
        [JsonProperty("countryRegion")]
        public string CountryRegion { get; set; }

        /// <summary>
        /// Gets or sets the country/region iso2.
        /// </summary>
        [JsonProperty("countryRegionIso2")]
        public string CountryRegionIso2 { get; set; }

        /// <summary>
        /// Gets or sets the locality.
        /// </summary>
        [JsonProperty("locality")]
        public string Locality { get; set; }

        /// <summary>
        /// Gets or sets the postal code.
        /// </summary>
        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }
    }
}