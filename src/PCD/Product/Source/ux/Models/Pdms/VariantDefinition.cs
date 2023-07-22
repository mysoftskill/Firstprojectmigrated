using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents a variant.
    /// </summary>
    public class VariantDefinition
    {
        /// <summary>
        /// Get or sets ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets owner ID.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets approver.
        /// </summary>
        public string Approver { get; set; }

        /// <summary>
        /// Gets or sets capabilities as strings.
        /// </summary>
        public IEnumerable<string> Capabilities { get; set; }

        /// <summary>
        /// Gets or sets data types as strings.
        /// </summary>
        public IEnumerable<string> DataTypes { get; set; }

        /// <summary>
        /// Gets or sets subject types as strings.
        /// </summary>
        public IEnumerable<string> SubjectTypes { get; set; }
    }
}
