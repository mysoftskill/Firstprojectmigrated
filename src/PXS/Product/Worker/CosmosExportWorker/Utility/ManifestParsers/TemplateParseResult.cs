// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    /// <summary>
    ///     result of parsing a manifest file
    /// </summary>
    public class TemplateParseResult
    {
        /// <summary>
        ///     Gets or sets the index of the pattern that matches the manifest file
        /// </summary>
        public int PatternIndex { get; set; }

        /// <summary>
        ///     Gets or sets the year
        /// </summary>
        public string Year { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the month
        /// </summary>
        public string Month { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the day
        /// </summary>
        public string Day { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the hour
        /// </summary>
        public string Hour { get; set; } = string.Empty;
    }
}
