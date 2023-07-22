using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Search
{
    /// <summary>
    /// Search result type that indicates complete failure to retrieve search result.
    /// </summary>
    public class SearchErrorResult : SearchResultBase
    {
        /// <summary>
        /// Always returns true, which indicates error.
        /// </summary>
        public bool IsError { get; } = true;
    }
}
