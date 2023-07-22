namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Standard properties that are logged for all data access results.
    /// </summary>
    public class DataAccessResult
    {
        /// <summary>
        /// Gets or sets the key (id(s), filter criteria or asset qualifier) used to lookup the data.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the total number of hits for search by id or filter, or 
        /// set to 1 if the pending commands or linked entity check returns true.
        /// </summary>
        public int TotalHits { get; set; }
    }
}