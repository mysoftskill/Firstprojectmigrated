// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    /// <summary>
    /// Properties of a partner adapter
    /// </summary>
    public class PartnerAdapter
    {
        /// <summary>
        /// Partner Id
        /// </summary>
        public string PartnerId { get; set; }

        /// <summary>
        /// Adapter instance
        /// </summary>
        public IPxfAdapter Adapter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the partner adapter supports real-time-delete.
        /// </summary>
        public bool RealTimeDelete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the partner adapter supports real-time-view.
        /// </summary>
        public bool RealTimeView { get; set; }
    }
}
