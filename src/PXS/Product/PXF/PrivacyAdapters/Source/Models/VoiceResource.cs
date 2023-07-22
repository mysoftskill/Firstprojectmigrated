// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    /// <summary>
    /// Voice resource
    /// </summary>
    public sealed class VoiceResource : Resource
    {
        /// <summary>
        /// The translated text of the voice utterance.
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// Gets or sets the application for the event.
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// Gets or sets the device type for the event.
        /// </summary>
        public string DeviceType { get; set; }
    }
}
