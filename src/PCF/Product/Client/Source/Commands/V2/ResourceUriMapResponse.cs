namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    /// <summary>
    /// Response payload for resourceURIMap API
    /// </summary>
    public class ResourceUriMapResponse
    {
        /// <summary>
        /// The id of the current page.
        /// </summary>
        public int PageId { get; set; }
        /// <summary>
        /// The total number of pages in this agentId-assetGroupId combination.
        /// </summary>
        public int TotalPages { get; set; }
        /// <summary>
        /// The link to the next page. Null if this is the last page.
        /// </summary>
        public string NextLink { get; set; }
        /// <summary>
        /// The content of the ResourceURIMap csv file
        /// </summary>
        public string ResourceURIMapPage { get; set; }
    }
}
