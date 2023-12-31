﻿// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Microsoft.Osgs.ServiceClient.Compass;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Resolvers
{
    public class TargetingTagResolver : ICompassTargetingTagResolver
    {
        /// <summary>
        /// Targeting tag to identify this website (versus BAM or ALC)
        /// </summary>
        public const string SiteIdTargeting = "Site_AMC";

        /// <summary>
        /// Prefix used for area name tags in compass
        /// </summary>
        private const string AreaPrefix = "Area_";

        /// <summary>
        /// Prefix used for page id tags in compass
        /// </summary>
        private const string PageIdPrefix = "PageId_";

        /// <summary>
        /// Get the compass tag for the given area name. This is an optional tag that should only be
        /// included in requests to cms that need the tag. It is currently used by the SharedShellService
        /// which takes Compass targeting tags as a parameter
        /// </summary>
        /// <param name="areaName">The area name to get the tag for</param>
        /// <returns>The compass tag for the given area name</returns>
        public static string GetAreaTag(string areaName)
        {
            return string.Concat(AreaPrefix, areaName);
        }

        /// <summary>
        /// Get the compass tag for the given page id. This is an optional tag that should only be
        /// included in requests to cms that need the tag. It is currently used by the SharedShellService
        /// which takes Compass targeting tags as a parameter
        /// </summary>
        /// <param name="pageId">The page id to get the tag for</param>
        /// <returns>The compass tag for the given page id</returns>
        public static string GetPageIdTag(string pageId)
        {
            return string.Concat(PageIdPrefix, pageId);
        }

        /// <summary>
        /// Gets the targeting tags for the request. IMPORTANT: this method could be invoked concurrently by different threads, it needs to be thread safe.
        /// </summary>
        /// <returns>A list of targeting tags.</returns>
        public List<string> GetTargetingTags()
        {
            //return UserSession.Current.Variants.CompassTags.ToList();
            return new List<string>();
        }

        /// <summary>
        /// Sets the targeting tags for the resolver. If never called, tags are generated by default (see GetTargetingTags)
        /// </summary>
        /// <param name="tags">A list of targeting tags.</param>
        public void SetTargetingTags(IList<string> tags)
        {
            ////This is no op because the target tags should be evaluated for every request
        }
    }
}