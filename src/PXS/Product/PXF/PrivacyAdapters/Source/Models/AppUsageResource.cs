// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System;

    /// <summary>
    ///     AppUsage resource
    /// </summary>
    public sealed class AppUsageResource : Resource
    {
        /// <summary>
        ///     Gets or sets the aggregation of the resources. (Daily or Monthly)
        /// </summary>
        public string Aggregation { get; set; }

        /// <summary>
        ///     Gets or sets the application icon background color in html code format (e.g. #334455).
        /// </summary>
        public string AppIconBackground { get; set; }

        /// <summary>
        ///     Gets or sets the application icon url.
        /// </summary>
        public string AppIconUrl { get; set; }

        /// <summary>
        ///     Gets or sets the application id.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        ///     Gets or sets the application name.
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        ///     Gets or sets the application publisher.
        /// </summary>
        public string AppPublisher { get; set; }

        /// <summary>
        ///     Gets or sets the end time.
        /// </summary>
        public DateTimeOffset EndDateTime { get; set; }
    }
}
