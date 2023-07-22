using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrivacyPolicy = Microsoft.PrivacyServices.Policy;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    /// <summary>
    /// Implements privacy policy accessor.
    /// </summary>
    public class PrivacyPolicyAccessor : IPrivacyPolicyAccessor
    {
        #region IPrivacyPolicyAccessor Members

        public PrivacyPolicy.Policy Get() => PrivacyPolicy.Policies.Current;

        #endregion
    }
}
