//--------------------------------------------------------------------------------
// <copyright file="LoggingHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common.Configuration;

    /// <summary>
    ///     provides a filter for certain logging operations
    /// </summary>
    public class LoggingFilter : ILoggingFilter
    {
        private readonly IList<string> filter;

        /// <summary>
        ///     Initializes a new instance of the LoggingFilter class
        /// </summary>
        /// <param name="config">configuration</param>
        public LoggingFilter(IPrivacyConfigurationManager config)
        {
            this.filter = config?.AdaptersConfiguration?.HttpRequestLoggingFilter?.IdsForExtendedLogging;
            if (this.filter != null && this.filter.Count == 0)
            {
                this.filter = null;
            }
        }

        /// <summary>
        ///     Returns a value indicating whether to log request and response details for the specified identity
        /// </summary>
        /// <param name="identity">identity to check</param>
        /// <returns>true to log details; false otherwise</returns>
        public bool ShouldLogDetailsForUser(string identity)
        {
            if (this.filter != null && string.IsNullOrWhiteSpace(identity) == false)
            {
                return this.filter.Any(s => s.Equals(identity, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }
    }
}
