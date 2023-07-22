using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Osgs.Core.Helpers;

//#error Please add examples of keys and what they look like after transformation by each method.

namespace Microsoft.PrivacyServices.UX.Core.Cms.Services
{
    public class CmsKeyParser : ICmsKeyParser
    {
        /// <summary>
        /// Parse <see cref="CmsKey" /> to determine CMS content type and location in Compass
        /// 
        /// Example 1: 
        /// CmsKey with AreaName = "agentHealth" CmsId = "page.agent-health"  
        ///     maps to compass location: /agentHealth/agent-health"
        ///     and compass type: page
        ///
        /// Example 2:
        /// CmsKey with AreaName = "agentHealth" CmsId = "page.special.agent-health"  
        ///     maps to compass location: /agentHealth/agent-health"
        ///     and compass type: page.special
        ///
        /// Note: All locations are relative to PrivacyComplianceDashboard node in Compass.
        /// </summary>
        /// <param name="cmsKey">CmsKey that needs to be parsed.</param>
        /// <returns>Parsed <see cref="ParsedCmsKey" /></returns>
        public ParsedCmsKey Parse(CmsKey cmsKey)
        {
            EnsureArgument.NotNullOrWhiteSpace(cmsKey.CmsId, nameof(cmsKey.CmsId));

            var separators = new string[] { "." };
            var tokens = cmsKey.CmsId.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            EnsureArgument.Is(tokens.Length >= 2, "CmsId must contain Content Type and Name separated by dot, e.g.: page.agent-health");

            var cmsContentItemName = tokens[tokens.Length - 1];
            var areaPath = (string.IsNullOrWhiteSpace(cmsKey.AreaName) ? string.Empty : $"/{cmsKey.AreaName}");

            return new ParsedCmsKey
            {
                CmsLocation = $"{areaPath}/{cmsContentItemName}",
                CmsTypeName = string.Join(".", tokens.Take(tokens.Length - 1))
            };
        }

    }
}
