using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PdmsModels = Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    /// <summary>
    /// Provides access to the PDMS Variant cache instance.
    /// </summary>
    public interface IVariantNameCache
    {
        /// <summary>
        /// Gets the name of the variant.
        /// </summary>
        Task<string> GetVariantName(string variantId);

        /// <summary>
        /// Gets the variant names for the variant IDs.
        /// </summary>
        Task<IReadOnlyDictionary<string, string>> GetVariantNamesAsync(IEnumerable<string> variantIds);
    }
}
