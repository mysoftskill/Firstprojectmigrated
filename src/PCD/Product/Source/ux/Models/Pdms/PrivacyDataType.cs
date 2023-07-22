using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Defines privacy data type.
    /// </summary>
    public class PrivacyDataType
    {
        /// <summary>
        /// Gets or sets ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets name (not localized).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets description (not localized).
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a list of capability IDs supported by the data type.
        /// </summary>
        public IEnumerable<string> Capabilities { get; set; }
    }
}
