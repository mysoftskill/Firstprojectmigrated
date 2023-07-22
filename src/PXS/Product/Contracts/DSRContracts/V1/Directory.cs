// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1
{
    using System.Collections.Generic;

    /// <summary>
    ///     Directory.
    /// </summary>
    public class Directory
    {
        /// <summary>
        ///     Gets or sets the inbound SharedUserProfile items.
        /// </summary>
        public ICollection<InboundSharedUserProfile> InboundSharedUserProfiles { get; set; }

        /// <summary>
        ///     Gets or sets the outbound SharedUserProfile items.
        /// </summary>
        public ICollection<OutboundSharedUserProfile> OutboundSharedUserProfiles { get; set; }

        /// <summary>
        ///     Gets or sets the deleted items.
        /// </summary>
        public ICollection<DeletedItem> DeletedItems { get; set; }

        /// <summary>
        ///     Gets or sets Id.
        /// </summary>
        public string Id { get; set; }
    }
}
