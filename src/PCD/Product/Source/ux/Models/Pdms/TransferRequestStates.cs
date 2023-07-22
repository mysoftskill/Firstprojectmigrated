using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    public enum TransferRequestStates
    {
        None,
        Pending,
        Approving,
        Approved,
        Cancelled,
        Failed
    }
}
