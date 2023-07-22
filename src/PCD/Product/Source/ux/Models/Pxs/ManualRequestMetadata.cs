using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pxs
{
    /// <summary>
    /// Metadata from the client describing the manual request for reporting purposes.
    /// </summary>
    public class ManualRequestMetadata
    {
        /// <summary>
        /// Gets or sets the CAP ID.
        /// </summary>
        public string CapId { get; set; }

        /// <summary>
        /// Gets or sets the country of residence for the subject of the CAP request.
        /// </summary>
        public string CountryOfResidence { get; set; }

        /// <summary>
        /// Gets or sets the priority in which the request should be handled.
        /// </summary>
        public string Priority { get; set; }

        /// <summary>
        /// Gets or sets the name (email) of the submitter of the request.
        /// </summary>
        public string ManualRequestSubmitter { get; set; }
    }
}
