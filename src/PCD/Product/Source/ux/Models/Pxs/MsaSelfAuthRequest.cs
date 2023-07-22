using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pxs
{
    /// <summary>
    /// Defines a MSA subject.
    /// </summary>
    public class MsaSelfAuthRequest
    {
        public MsaSelfAuthSubject Subject { get; set; }

        public ManualRequestMetadata Metadata { get; set; }

        public class MsaSelfAuthSubject
        {
            /// <summary>
            /// Gets or sets the MSA proxy ticket.
            /// </summary>
            public string ProxyTicket { get; set; }
        }
    }
}
