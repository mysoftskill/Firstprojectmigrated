// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers;

    /// <summary>
    ///     processes the data file manifest and returns the list of data files
    /// </summary>
    public static class DataFileManifestReader
    {
        // we'll attempt to parse the file into one of the following 3 forms (in this order) and extract the DataFilePrefix
        //  if all fail, we'll just use the filename as-is.
        //  <DataFilePrefix>_%Y_%M_%D_%H.txt
        //  <DataFilePrefix>_%Y_%M_%D.txt
        //  <DataFilePrefix>.txt

        /// <summary>
        ///     data file name regex set
        /// </summary>
        internal static readonly IReadOnlyList<Regex> DataFileNameRegexSet = new
            ReadOnlyCollection<Regex>(
                new[]
                {
                    new Regex(@"^(?<fileName>.*)_\d+_\d+_\d+_\d+\.txt$"),
                    new Regex(@"^(?<fileName>.*)_\d+_\d+_\d+\.txt$"),
                    new Regex(@"^(?<fileName>.*).txt$")
                });

        /// <summary>
        ///     Gets the list of data files in the data manifest file
        /// </summary>
        /// <param name="manifest">data manifest</param>
        /// <param name="manifestParseResult">manifest parse result</param>
        /// <param name="errorLogger">error logger</param>
        /// <returns>resulting value</returns>
        public static async Task<ICollection<ManifestDataFile>> GetDataFileNamesAsync(
            IFile manifest,
            TemplateParseResult manifestParseResult,
            Utility.TraceLoggerAction errorLogger)
        {
            IDictionary<string, ManifestDataFile> result;
            ISet<string> fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Stream contents;
            int uniqueifier = 0;

            result = new Dictionary<string, ManifestDataFile>(StringComparer.OrdinalIgnoreCase);

            // next, fetch the data manifest file and build a list of all command files
            contents = manifest.GetDataReader();

            if (contents != null)
            {
                using (StreamReader sr = new StreamReader(contents))
                {
                    int row = 0;

                    while (sr.EndOfStream == false)
                    {
                        string current = await sr.ReadLineAsync().ConfigureAwait(false);

                        try
                        {
                            string[] columns = current?.Split(new[] { '\t' }, StringSplitOptions.None);
                            if (columns?.Length >= 1)
                            {
                                ManifestDataFile item;
                                string packageName;
                                string cosmosName;
                                string rawName = columns[0].Trim();

                                if (string.IsNullOrWhiteSpace(rawName))
                                {
                                    continue;
                                }

                                if (result.TryGetValue(rawName, out item))
                                {
                                    item.CountFound += 1;
                                    continue;
                                }

                                cosmosName = ManfiestNameParser.BuildOutputFilenameFromTemplate(rawName, manifestParseResult);

                                (packageName, uniqueifier) = Utility.ExtractDataFileName(
                                    DataFileManifestReader.DataFileNameRegexSet,
                                    fileNames,
                                    cosmosName,
                                    uniqueifier,
                                    manifestParseResult.PatternIndex);

                                item = new ManifestDataFile(columns[0], cosmosName, packageName);

                                // this is the known set of invalid characters
                                if (cosmosName.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '%' }) >= 0)
                                {
                                    item.Invalid = true;
                                }

                                result.Add(rawName, item);
                                continue;
                            }

                            errorLogger(
                                $"Failed to process row {row} in data manifest file {manifest.Name}. Row is [{current}]");
                        }
                        finally
                        {
                            ++row;
                        }
                    }
                }
            }

            return result.Values;
        }
    }
}
