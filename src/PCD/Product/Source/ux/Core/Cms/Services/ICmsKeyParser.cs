using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Services
{
    /// <summary>
    /// Responsible for validates/parsing to CmsKey and derive information from it.
    /// </summary>
    public interface ICmsKeyParser
    {
        /// <summary>
        /// Parse CmsKey to determine CMS content type and location in Compass.
        /// </summary>
        /// <param name="cmsKey">CmsKey that needs to be parsed.</param>
        /// <returns>Parsed <see cref="ParsedCmsKey" /></returns>
        ParsedCmsKey Parse(CmsKey cmsKey);
    }
}
