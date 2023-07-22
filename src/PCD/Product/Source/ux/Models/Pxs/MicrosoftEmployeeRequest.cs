using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pxs
{
    /// <summary>
    /// Defines a Microsoft employee subject.
    /// </summary>
    public class MicrosoftEmployeeRequest
    {
        public MicrosoftEmployeeSubject Subject { get; set; }

        public ManualRequestMetadata Metadata { get; set; }

        public class MicrosoftEmployeeSubject
        {
            /// <summary>
            /// Gets or sets emails.
            /// </summary>
            public IEnumerable<string> Emails { get; set; }

            /// <summary>
            /// Gets or sets employee ID.
            /// </summary>
            public string EmployeeId { get; set; }

            /// <summary>
            /// Gets or sets employment start date.
            /// </summary>
            public DateTimeOffset EmploymentStartDate { get; set; }

            /// <summary>
            /// Gets or sets employment end date.
            /// </summary>
            public DateTimeOffset? EmploymentEndDate { get; set; }
        }
    }
}
