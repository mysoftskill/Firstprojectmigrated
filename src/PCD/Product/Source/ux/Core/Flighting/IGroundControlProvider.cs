using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Flighting
{
    /// <summary>
    /// Provides access to Ground Control instance.
    /// </summary>
    public interface IGroundControlProvider
    {
        /// <summary>
        /// Gets an instance of Ground Control object.
        /// </summary>
        IGroundControl Instance
        {
            get;
        }
    }
}
