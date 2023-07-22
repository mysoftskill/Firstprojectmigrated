using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Services
{
    /// <summary>
    /// CmsKey uniquely identifies a content in Compass
    /// </summary>
    public class CmsKey : IEquatable<CmsKey>
    {
        /// <summary>
        /// AreaName maps to Compass folder name (currently sub-folders are not supported).
        /// Empty Area names are valid, which will point to root of PrivacyComplianceDashboard node.
        /// e.g. "agent-health" or "shared"
        /// </summary>
        public string AreaName { get; set; }

        /// <summary>
        /// CmsId dot separated string containing content type and item name in Compass.
        //  Last segment of CmsId determines Content Item Name is Compass.
        /// Everything except last segment of CmsId determines the type of CMS.
        /// e.g. "page.agent-health" or "component.shared-content"
        /// </summary>
        public string CmsId { get; set; }

        /// <summary>
        /// Unique key for the CMSKey, used by TS client as identifier
        /// </summary>
        public string CompositeKey => $"{CmsId}@{AreaName}".ToUpperInvariant();

        /// <summary>
        /// Compares this CmsKey with other CmsKey for equality
        /// </summary>
        /// <param name="other">Other CmsKey to compare.</param>
        /// <returns>returns true when match, false when does not match.</returns>
        public bool Equals(CmsKey other)
        {
            return string.Equals(CompositeKey, other.CompositeKey, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Get Hash Code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return CompositeKey.GetHashCode();
        }
    }
}
