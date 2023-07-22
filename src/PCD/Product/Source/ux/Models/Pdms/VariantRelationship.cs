namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents a variant relationship
    /// </summary>
    public class VariantRelationship
    {
        /// Gets or sets data asset group id
        /// </summary>
        public string AssetGroupId { get; set; }

        /// <summary>
        /// Gets or sets data asset group qualifier.
        /// </summary>
        public AssetGroupQualifier AssetGroupQualifier { get; set; }
    }
}
