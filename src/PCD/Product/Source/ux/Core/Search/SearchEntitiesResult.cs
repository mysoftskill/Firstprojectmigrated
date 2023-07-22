using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Search
{
    /// <summary>
    /// Search result that returns a list of found entities.
    /// </summary>
    public class SearchEntitiesResult : SearchResultBase
    {
        /// <summary>
        /// Gets or sets a list of entities found in PDMS. Can be null, if nothing was found.
        /// </summary>
        public IEnumerable<SearchResultBase> Entities { get; set; }
    }
}
