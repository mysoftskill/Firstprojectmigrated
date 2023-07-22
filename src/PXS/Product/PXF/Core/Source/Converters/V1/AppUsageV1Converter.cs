// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Converters
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    /// AppUsageV1Converter Converter
    /// </summary>
    public static class AppUsageV1Converter
    {
        /// <summary>
        /// Converts a collection of <see cref="AppUsageResource"/> to a collection of <see cref="AppUsageV1"/>.
        /// </summary>
        /// <param name="appUsageResources">The AppUsageResource resources.</param>
        /// <returns>A collection of <see cref="AppUsageV1"/></returns>
        internal static List<AppUsageV1> ToAppUsageV1(this IEnumerable<AppUsageResource> appUsageResources)
        {
            if (appUsageResources == null)
            {
                return null;
            }

            var appUsagesV1 = new List<AppUsageV1>();

            foreach (var appUsageResource in appUsageResources)
            {
                appUsagesV1.Add(appUsageResource.ToAppUsageV1());
            }

            return appUsagesV1;
        }

        /// <summary>
        /// Converts <see cref="AppUsageResource"/> to <see cref="AppUsageV1"/>.
        /// </summary>
        /// <param name="appUsageResource">The AppUsageResource resource.</param>
        /// <returns>AppUsageV1</returns>
        public static AppUsageV1 ToAppUsageV1(this AppUsageResource appUsageResource)
        {
            AppUsageV1 appUsageV1 = new AppUsageV1
            {
                DateTime = appUsageResource.DateTime,
                DeviceId = appUsageResource.DeviceId,
                Id = appUsageResource.Id,
                Ids = new List<string> { appUsageResource.Id },
                IsAggregate = false,
                Source = appUsageResource.Sources?.FirstOrDefault(),
                PartnerId = appUsageResource.PartnerId,
                AppIconBackground = appUsageResource.AppIconBackground,
                AppIconUrl = appUsageResource.AppIconUrl,
                AppId = appUsageResource.AppId,
                AppName = appUsageResource.AppName,
                AppPublisher = appUsageResource.AppPublisher,
            };

            return appUsageV1;
        }
    }
}