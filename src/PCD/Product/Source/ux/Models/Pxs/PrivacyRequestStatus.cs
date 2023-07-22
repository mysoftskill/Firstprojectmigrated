using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pxs
{
    public class PrivacyRequestStatus
    {
        public string Context { get; set; }

        public Uri DestinationUri { get; set; }

        public Guid Id { get; set; }

        public PrivacyRequestState State { get; set; }

        public string SubjectType { get; set; }

        public DateTimeOffset SubmittedTime { get; set; }

        public DateTimeOffset? CompletedTime { get; set; }

        public int? Progress { get; set; }
    }

    public enum PrivacyRequestState
    {
        Submitted = 0,

        Completed = 1
    }
}
