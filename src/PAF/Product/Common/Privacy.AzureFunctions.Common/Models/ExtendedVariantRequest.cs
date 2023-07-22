namespace Microsoft.PrivacyServices.AzureFunctions.Common.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines a request to link a set of variants to a collection of asset groups with more information for ADO WorkItem.
    /// </summary>
    public class ExtendedVariantRequest : VariantRequest
    {
        /// <summary>
        /// List of Variants in the variant request
        /// </summary>
        public new IEnumerable<ExtendedAssetGroupVariant> RequestedVariants { get; set; }
    }
}
