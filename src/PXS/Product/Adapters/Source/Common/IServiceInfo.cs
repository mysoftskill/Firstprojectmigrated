// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    /// <summary>
    /// Information about the service running on this machine.
    /// </summary>
    public interface IServiceInfo
    {
        /// <summary>
        /// The version of the service.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// The user agent value to send in partner service requests.
        /// </summary>
        string UserAgent { get; }
    }
}
