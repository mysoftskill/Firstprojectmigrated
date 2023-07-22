// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;

    /// <summary>
    ///     misc utility methods used in multiple places in code with no other good location
    /// </summary>
    public static class Utility
    {
        /// <summary>
        ///     functon signature for sending trace logger messages
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement args</param>
        public delegate void TraceLoggerAction(
            string format,
            params object[] args);

        /// <summary>
        ///     Ensures that the path has no leading slash character
        /// </summary>
        /// <param name="path">path to check</param>
        /// <returns>resulting value</returns>
        public static string EnsureNoLeadingSlash(string path)
        {
            path = path?.Trim();

            return string.IsNullOrEmpty(path) ? path : (path[0] != '/' ? path : path.Substring(1));
        }

        /// <summary>
        ///     Ensures that the path has no leading slash character
        /// </summary>
        /// <param name="path">path to check</param>
        /// <returns>resulting value</returns>
        public static string EnsureTrailingSlash(string path)
        {
            path = path?.Trim();

            return string.IsNullOrEmpty(path) ? path : (path[path.Length - 1] == '/' ? path : path + "/");
        }

        /// <summary>
        ///     Ensures that the path has no leading slash character
        /// </summary>
        /// <param name="path">path to check</param>
        /// <returns>resulting value</returns>
        public static string EnsureHasTrailingSlashButNoLeadingSlash(string path)
        {
            path = path?.Trim();

            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (path[0] == '/')
            {
                path = path.Substring(1);
            }

            return path[path.Length - 1] == '/' ? path : path + "/";
        }

        /// <summary>
        ///     Canonicalizes the command id
        /// </summary>
        /// <param name="id">command id</param>
        /// <returns>resulting value</returns>
        public static string CanonicalizeCommandId(string id)
        {
            return id.Replace("-", string.Empty).ToLowerInvariant();
        }

        /// <summary>
        ///     Extracts the the year / month / day / hour from a manifest filename
        /// </summary>
        /// <param name="patterns">ordered list of patterns to use</param>
        /// <param name="fileName">template file name</param>
        /// <returns>(extracted name, updated uniqueifier, index of pattern used to extract the name)</returns>
        public static TemplateParseResult ParseTemplateFileName(
            IReadOnlyList<Regex> patterns,
            string fileName)
        {
            for (int idxPattern = 0; idxPattern < patterns.Count; ++idxPattern)
            {
                Regex regex = patterns[idxPattern];
                Match m = regex.Match(fileName);
                if (m.Success)
                {
                    string temp = m.Groups["fileName"].Value;
                    if (string.IsNullOrWhiteSpace(temp) == false)
                    {
                        return new TemplateParseResult
                        {
                            PatternIndex = idxPattern,
                            Year = m.Groups["year"].Value,
                            Month = m.Groups["month"].Value,
                            Day = m.Groups["day"].Value,
                            Hour = m.Groups["hour"].Value,
                        };
                    }
                }
            }

            return new TemplateParseResult { PatternIndex = patterns.Count };
        }

        /// <summary>
        ///     Extracts the file name for a data file (skipping the date/time suffix) using an ordered list of patterns
        /// </summary>
        /// <param name="patterns">ordered list of patterns to use</param>
        /// <param name="currentFiles">current files</param>
        /// <param name="dataFileName">data file name</param>
        /// <param name="uniqueifier">uniqueifier suffix to ensure unique filenames</param>
        /// <param name="startPatternIndex">pattern index to start matching at</param>
        /// <returns>(extracted name, updated uniqueifier)</returns>
        public static (string, int) ExtractDataFileName(
            IReadOnlyList<Regex> patterns,
            ISet<string> currentFiles,
            string dataFileName,
            int uniqueifier,
            int startPatternIndex)
        {
            // default to using the entire name as the name in the export package.  This should really never get used unless the 
            //  cosmos file is badly named
            string resultFinal = dataFileName;
            string resultBase = dataFileName;
            int idxPattern;

            for (idxPattern = startPatternIndex; idxPattern < patterns.Count; ++idxPattern)
            {
                Regex regex = patterns[idxPattern];
                Match m = regex.Match(dataFileName);
                if (m.Success)
                {
                    string temp = m.Groups["fileName"].Value;
                    if (string.IsNullOrWhiteSpace(temp) == false)
                    {
                        resultBase = resultFinal = temp;
                        break;
                    }
                }
            }

            // there will always be a finite number of files we process, so this will eventually terminate
            while (currentFiles.Contains(resultFinal))
            {
                resultFinal = resultBase + uniqueifier++.ToStringInvariant();
            }

            currentFiles.Add(resultFinal);

            return (resultFinal, uniqueifier);
        }

        /// <summary>
        ///     Generates the data file tag
        /// </summary>
        /// <param name="cosmosTag">cosmos tag</param>
        /// <param name="agentId">agent id</param>
        /// <param name="cosmosFileName">cosmos file name</param>
        /// <returns>a tag representing the file in cosmos and its owning agent</returns>
        public static string GenerateFileTag(
            string cosmosTag,
            string agentId,
            string cosmosFileName)
        {
            return cosmosTag + "." + agentId + "." + cosmosFileName;
        }

        /// <summary>
        ///     Generates the data file tag
        /// </summary>
        /// <param name="fileTag">file tag</param>
        /// <returns>tag representing the file in cosmos and its owning agent</returns>
        public static (string CosmosTag, string AgentId, string Name) SplitFileTag(string fileTag)
        {
            const int ExpectedParts = 3;

            string[] parts;

            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(fileTag, nameof(fileTag));

            // take only the first 3 parts as the filename could have embedded periods
            parts = fileTag.Split(new[] { '.' }, ExpectedParts, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != ExpectedParts)
            {
                throw new ArgumentException(
                    $"Only found {parts.Length} tag parts instead of the expected {ExpectedParts} in [{fileTag}]",
                    nameof(fileTag));
            }

            return (parts[0], parts[1], parts[2]);
        }

        /// <summary>
        ///     Generates the data file tag
        /// </summary>
        /// <param name="cosmosTag">cosmos tag</param>
        /// <param name="agentId">agent id</param>
        /// <param name="cosmosUri">cosmos file uri</param>
        /// <returns>a tag representing the file in cosmos and its owning agent</returns>
        public static string GenerateFileTagFromUri(
            string cosmosTag,
            string agentId,
            string cosmosUri)
        {
            string file = Path.GetFileName(cosmosUri);
            return string.IsNullOrWhiteSpace(file) ? cosmosUri : cosmosTag + "." + agentId + "." + file;
        }

        /// <summary>
        ///     Gets the file size based queue partition to send the data file to
        /// </summary>
        /// <param name="thresholds">config defined thresholds</param>
        /// <param name="size">file size</param>
        /// <returns>resulting value</returns>
        public static FileSizePartition GetPartition(
            ICosmosFileSizeThresholds thresholds,
            long size)
        {
            if (size == 0)
            {
                return FileSizePartition.Empty;
            }

            if (size > thresholds.Oversized)
            {
                return FileSizePartition.Oversize;
            }

            if (size > thresholds.Large)
            {
                return FileSizePartition.Large;
            }

            return size > thresholds.Medium ? FileSizePartition.Medium : FileSizePartition.Small;
        }

        /// <summary>
        ///     Extracts the status code from HTTP request exception data
        /// </summary>
        /// <param name="exception">exception to extract status code from</param>
        /// <returns>status code or null if no code could be found</returns>
        public static int? ExtractStatusCodeFromHttpRequestExceptionData(HttpRequestException exception)
        {
            int? result = null;

            if (exception.Data.Contains("StatusCode"))
            {
                object obj = exception.Data["StatusCode"];

                if (obj is HttpStatusCode)
                {
                    HttpStatusCode code = (HttpStatusCode)obj;
                    result = (int)code;
                }
                else
                {
                    try
                    {
                        result = Convert.ToInt32(obj);
                    }
                    catch (Exception e2) when (e2 is FormatException || e2 is OverflowException)
                    {
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Gets the hash code for an unordered collection of items based on the contents of the collection
        /// </summary>
        /// <typeparam name="T">type of item the collection is comprised of</typeparam>
        /// <param name="collection">collection to get the hash code for</param>
        /// <returns>resulting value</returns>
        /// <remarks>this routine does not dedupe the collection and </remarks>
        public static int GetHashCodeForUnorderedCollection<T>(ICollection<T> collection)
        {
            const int HashPrime = 251;

            int result = 0;
           
            if (collection?.Count > 0)
            {
                // a collection of one item is always going to be just the hash code for that one item
                if (collection.Count == 1)
                {
                    return collection.First()?.GetHashCode() ?? 0;
                }

                // ordering a list of ints is cheaper than ordering a list of an arbitrary object (say strings) as the cost
                //  of comparing ints is about the cheapest you can get (again, compared to things like strings)- we want 
                //  the hash codes for each object anyway, so we'd pay the cost to compute them regardless.
                List<int> hashes = collection.Select(o => o?.GetHashCode() ?? 0).ToList();
                hashes.Sort();

                // merge the hash codes
                foreach (int hash in hashes)
                {
                    // yes, we want to allow overflow
                    unchecked
                    {
                        result *= HashPrime;
                        result += hash; 
                    }
                }
            }

            return result;
        }
    }
}