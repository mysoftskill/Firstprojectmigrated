namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Variants approved by CELA that can be claimed by the agent
    /// </summary>
    public class Variant
    {
        /// <summary>
        /// Guid value of the Variant Id
        /// </summary>
        public string VariantId { get; set; }

        /// <summary>
        /// Name of the variant
        /// </summary>
        public string VariantName { get; set; }

        /// <summary>
        /// Variant Description
        /// </summary>
        public string VariantDescription { get; set; }

        /// <summary>
        /// Variant AssetQualifier
        /// </summary>
        public string AssetQualifier { get; set; }

        /// <summary>
        /// DatatypeIds covered by the variant
        /// </summary>
        public IEnumerable<DataTypeId> DataTypeIds { get; set; } = new DataTypeId[0];
    }
}
