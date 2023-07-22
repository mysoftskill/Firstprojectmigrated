namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    /// <summary>
    /// Properties to use for setting up the database.
    /// </summary>
    public class SetupProperties
    {
        /// <summary>
        /// Gets or sets a value to use as the database name.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets a value to use as the collection name.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Gets or sets the RUs for the collection.
        /// </summary>
        public int OfferThroughput { get; set; }
    }
}