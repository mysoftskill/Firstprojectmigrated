using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents privacy policy for UX.
    /// </summary>
    public sealed class PrivacyPolicy
    {
        /// <summary>
        /// Gets or sets a map of all known data types (ID to data type).
        /// </summary>
        public IReadOnlyDictionary<string, PrivacyDataType> DataTypes { get; set; }

        /// <summary>
        /// Gets or sets a map of all known capabilities (ID to capability).
        /// </summary>
        public IReadOnlyDictionary<string, PrivacyCapability> Capabilities { get; set; }

        /// <summary>
        /// Gets or sets a map all known protocols (ID to protocol).
        /// </summary>
        public IReadOnlyDictionary<string, PrivacyProtocol> Protocols { get; set; }
    }
}
