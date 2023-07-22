namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents data asset group.
    /// </summary>
    public class DataAsset
    {
        /// <summary>
        /// Gets or sets data asset group ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The set of properties that are necessary to uniquely identify a Data Asset group in DataGrid.
        /// </summary>
        public AssetGroupQualifier Qualifier { get; set; }
    }
}
