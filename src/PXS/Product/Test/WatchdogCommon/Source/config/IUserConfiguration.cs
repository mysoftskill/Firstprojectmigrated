//--------------------------------------------------------------------------------
// <copyright file="IUserConfiguration.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.WatchdogCommon.Config
{
    /// <summary>
    /// Miscellaneous user configuration data.
    /// </summary>
    public interface IUserConfiguration
    {
        /// <summary>
        /// Gets the user PUID.
        /// </summary>
        long Puid { get; }

        /// <summary>
        /// Gets the user CID.
        /// </summary>
        long Cid { get; }

        /// <summary>
        /// Gets the user-name.
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Gets the user-password.
        /// </summary>
        string Password { get; }

        /// <summary>
        /// Gets the user subscription-id.
        /// </summary>
        string GetSubscriptionHistorySubscriptionId { get; }
    }
}
