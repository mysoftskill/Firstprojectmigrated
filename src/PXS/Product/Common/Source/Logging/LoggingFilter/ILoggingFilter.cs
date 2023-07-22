//--------------------------------------------------------------------------------
// <copyright file="LoggingHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter
{
    public interface ILoggingFilter
    {
        /// <summary>
        ///     Returns a value indicating whether to log request and response details for the specified identity
        /// </summary>
        /// <param name="identity">identity to check</param>
        /// <returns>true to log details; false otherwise</returns>
        bool ShouldLogDetailsForUser(string identity);
    }
}
