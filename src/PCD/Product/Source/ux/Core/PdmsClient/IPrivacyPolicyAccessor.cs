using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrivacyPolicy = Microsoft.PrivacyServices.Policy;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    /// <summary>
    /// Defines accessor to privacy policy.
    /// </summary>
    public interface IPrivacyPolicyAccessor
    {
        /// <summary>
        /// Gets current instance of privacy policy.
        /// </summary>
        PrivacyPolicy.Policy Get();
    }
}
