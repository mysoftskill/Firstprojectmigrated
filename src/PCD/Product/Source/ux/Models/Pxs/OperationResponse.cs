using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pxs
{
    /// <summary>
    /// Represents privacy subject operation response.
    /// </summary>
    public class OperationResponse
    {
        /// <summary>
        /// Gets or sets a list of DSR IDs.
        /// </summary>
        public IEnumerable<string> Ids { get; set; }
    }
}
