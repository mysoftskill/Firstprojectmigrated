// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Manfiest name parser
    /// </summary>
    public static class ManfiestNameParser
    {
        private static readonly Regex FileMatchManifest4 =
            new Regex(@"^(?<fileName>.*)_(?<year>\d+)_(?<month>\d+)_(?<day>\d+)_(?<hour>\d+)\.txt$");

        private static readonly Regex FileMatchManifest3 =
            new Regex(@"^(?<fileName>.*)_(?<year>\d+)_(?<month>\d+)_(?<day>\d+)\.txt$");
        
        private static readonly Regex FileMatchTxt = new Regex(@"^(?<fileName>.*).txt$");

        private static readonly IReadOnlyList<Regex> ManifestFileNameRegexSet = new
            ReadOnlyCollection<Regex>(
                new[]
                {
                    ManfiestNameParser.FileMatchManifest4,
                    ManfiestNameParser.FileMatchManifest3,
                    ManfiestNameParser.FileMatchTxt
                });

        /// <summary>
        ///     Parses a data manifest filename for template parameters to assign to data files
        /// </summary>
        /// <param name="dataFileManifestName">data file manifest name</param>
        /// <returns>parse results</returns>
        public static TemplateParseResult ParseManifestForDataFileTemplate(string dataFileManifestName)
        {
            return Utility.ParseTemplateFileName(ManfiestNameParser.ManifestFileNameRegexSet, dataFileManifestName);
        }

        /// <summary>
        ///     Builds the output filename from the input template using the manifest parse result template
        /// </summary>
        /// <param name="templateName">template name</param>
        /// <param name="manifestParseResult">manifest parse result</param>
        /// <returns>output filename</returns>
        public static string BuildOutputFilenameFromTemplate(
            string templateName,
            TemplateParseResult manifestParseResult)
        {
            return new StringBuilder(templateName)
                .Replace("%Y", manifestParseResult.Year)
                .Replace("%y", manifestParseResult.Year)
                .Replace("%M", manifestParseResult.Month)
                .Replace("%m", manifestParseResult.Month)
                .Replace("%D", manifestParseResult.Day)
                .Replace("%d", manifestParseResult.Day)
                .Replace("%H", manifestParseResult.Hour)
                .Replace("%h", manifestParseResult.Hour).ToString();
        }

        /// <summary>
        ///     determines if a path requires fixing up based on the presence of replacement args
        /// </summary>
        /// <param name="path">path to check</param>
        /// <returns>true if it does, false otherwise</returns>
        public static bool RequiresFixup(string path)
        {
            return path.IndexOf("%y", StringComparison.Ordinal) >= 0 ||
                   path.IndexOf("%h", StringComparison.Ordinal) >= 0 ||
                   path.IndexOf("%m", StringComparison.Ordinal) >= 0 ||
                   path.IndexOf("%d", StringComparison.Ordinal) >= 0;
        }
    }
}
