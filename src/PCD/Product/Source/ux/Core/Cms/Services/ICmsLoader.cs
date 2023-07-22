using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Services
{
    /// <summary>
    /// Responsible for loading content from Compass for known Compass types and mapping it to C# compass types.
    /// </summary>
    public interface ICmsLoader
    {
        /// <summary>
        /// Loads Compass content for specified CmsKey and Culture code.
        /// </summary>
        /// <param name="key">CmsKey for which content needs to be fetched.</param>
        /// <param name="cultureCode">Content will be requested for this culture code.</param>
        /// <returns>CMS Content Item.</returns>
        Task<IBaseCompassType> LoadAsync(CmsKey key, string cultureCode);
    }
}
