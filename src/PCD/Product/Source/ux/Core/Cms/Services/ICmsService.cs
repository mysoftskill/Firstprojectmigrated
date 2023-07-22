using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Services
{
    /// <summary>
    /// Responsible for retrieving content from Compass, service performs any orchestration needed to retrieve the content.
    /// </summary>
    public interface ICmsService
    {
        /// <summary>
        /// Retrieves a CMS content item for a CmsKey and Culture.
        /// </summary>
        /// <param name="key">CmsKey for which content items need to be fetched.</param>
        /// <param name="cultureCode">Content will be requested for this culture code.</param>
        /// <returns>CMS content item.</returns>
        Task<IBaseCompassType> GetCmsContentItemAsync(CmsKey key, string cultureCode);

        /// <summary>
        /// Retrieves CMS content items for multiple CmsKeys and a specified Culture.
        /// </summary>
        /// <param name="keys">CmsKeys for which content items need to be fetched.</param>
        /// <param name="cultureCode">Content will be requested for this culture code.</param>
        /// <returns>CMS content items.</returns>
        Task<IDictionary<string, IBaseCompassType>> GetCmsContentItemsAsync(IEnumerable<CmsKey> keys, string cultureCode);
    }
}
