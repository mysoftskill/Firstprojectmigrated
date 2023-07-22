// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Post Export Request Args
    /// </summary>
    public class PostExportRequestArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     Gets or sets the Categories collection
        /// </summary>
        public IList<string> DataTypes { get; }

        /// <summary>
        ///     The end time for the exported data
        /// </summary>
        public DateTimeOffset EndTime { get; }

        /// <summary>
        ///     The start time for the exported data
        /// </summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PostExportRequestArgs" /> class.
        /// </summary>
        /// <param name="dataTypes"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        public PostExportRequestArgs(IList<string> dataTypes, DateTimeOffset startTime, DateTimeOffset endTime, string userProxyTicket)
            : base(userProxyTicket)
        {
            if (userProxyTicket == null)
                throw new ArgumentNullException(nameof(userProxyTicket));

            this.DataTypes = dataTypes ?? throw new ArgumentNullException(nameof(dataTypes));
            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        /// <summary>
        ///     Creates the appropriate query string collection
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return new QueryStringCollection
            {
                { "dataTypes", string.Join(",", this.DataTypes) },
                { "startTime", this.StartTime.ToString("o") },
                { "endTime", this.EndTime.ToString("o") }
            };
        }
    }
}
