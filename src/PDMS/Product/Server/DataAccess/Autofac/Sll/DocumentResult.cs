namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    /// <summary>
    /// Standard properties that are logged for all document client results.
    /// </summary>
    public class DocumentResult
    {
        /// <summary>
        /// Gets or sets the request uri.
        /// </summary>
        public string RequestUri { get; set; }

        /// <summary>
        /// Gets or sets the request charge.
        /// </summary>
        public double RequestCharge { get; set; }

        /// <summary>
        /// Gets or sets the activity id.
        /// </summary>
        public string ActivityId { get; set; }

        /// <summary>
        /// Gets or sets the count of items returned by a query.
        /// </summary>
        public int Count { get; set; }
    }
}