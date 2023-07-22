namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// A response from the service that is a collection.
    /// </summary>
    /// <typeparam name="T">The data type in this collection.</typeparam>
    public class Collection<T>
    {
        /// <summary>
        /// Gets or sets the response value.
        /// </summary>
        public IEnumerable<T> Value { get; set; }

        /// <summary>
        /// Gets or sets the total number of results for the query.
        /// </summary>
        [JsonProperty(PropertyName = "@odata.count")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the link used to retrieve the next batch for this query. This will only exist if the server returned fewer results than requested.
        /// </summary>
        [JsonProperty(PropertyName = "@odata.nextLink")]
        public Uri NextLink { get; set; }
    }
}