using Microsoft.Osgs.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Services
{
    /// <summary>
    /// Contains useful information for loading the Compass content.
    /// </summary>
    public class ParsedCmsKey
    {
        /// <summary>
        /// Points to location, relative to PrivacyComplianceDashBoard node in Compass.
        /// </summary>
        public string CmsLocation { get; set; }

        /// <summary>
        /// String representing type of CMS, used for mapping to C# Compass type.
        /// </summary>
        public string CmsTypeName { get; set; }

    }
}
