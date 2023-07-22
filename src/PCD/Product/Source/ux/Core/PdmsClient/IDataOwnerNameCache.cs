using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    /// <summary>
    /// Provides access to the PDMS Data Owner cache instance.
    /// </summary>
    public interface IDataOwnerNameCache
    {
        /// <summary>
        /// Gets the name of the data owner.
        /// </summary>
        Task<string> GetDataOwnerName(string dataOwnerId);

        /// <summary>
        /// Gets the data owner names for the data owner IDs.
        /// </summary>
        Task<IReadOnlyDictionary<string, string>> GetDataOwnerNamesAsync(IEnumerable<string> dataOwnerIds);
    }
}
