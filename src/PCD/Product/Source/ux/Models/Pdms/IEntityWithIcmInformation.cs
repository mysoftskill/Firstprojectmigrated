using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Contract for entities with IcM information.
    /// </summary>
    public interface IEntityWithIcmInformation
    {
        /// <summary>
        /// Gets or sets IcM connector ID.
        /// </summary>
        string IcmConnectorId { get; set; }
    }
}
