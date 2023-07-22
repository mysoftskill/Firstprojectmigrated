// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using VcClient;

    using VcClientExceptions;

    /// <summary>
    ///     Utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        ///     Enumerates the cosmos directory and performs actions on the elements
        /// </summary>
        /// <param name="path">name</param>
        /// <param name="filter">filter of item to include</param>
        /// <param name="fileProcessor">file processor</param>
        /// <param name="dirProcessor">dir processor</param>
        /// <returns>resulting value</returns>
        public static async Task EnumerateCosmosDirectoryAsync(
            string path,
            Func<StreamInfo, bool> filter = null,
            Func<StreamInfo, Task> fileProcessor = null,
            Func<StreamInfo, Task> dirProcessor = null)
        {
            List<StreamInfo> elements;

            try { elements = VC.GetDirectoryInfo(path, true); }
            catch (VcClientException) { elements = new List<StreamInfo>(); }

            foreach (StreamInfo info in elements)
            {
                if (filter != null && filter(info))
                {
                    continue;
                }

                try
                {
                    if (info.IsDirectory)
                    {
                        if (dirProcessor != null)
                        {
                            await dirProcessor(info);
                        }
                    }
                    else
                    {
                        if (fileProcessor != null)
                        {
                            await fileProcessor(info);
                        }
                    }
                }
                catch (VcClientException e)
                {
                    Console.WriteLine("Failed to process directory " + info.StreamName + ": " + e.ToString());
                    throw;
                }
            }
        }

        /// <summary>
        ///     Splits the name and name
        /// </summary>
        /// <param name="path">name</param>
        /// <returns>resulting value</returns>
        public static (string Path, string Name) SplitNameAndPath(string path)
        {
            int lastSlash = path.LastIndexOf('/');

            if (lastSlash > 0 && lastSlash == path.Length - 1)
            {
                lastSlash = path.LastIndexOf('/', lastSlash - 1);
            }

            return lastSlash < 0 ?
                ("/", path) :
                (path.Substring(0, lastSlash), path.Substring(lastSlash + 1));
        }

        /// <summary>
        ///     Splits manifest and suffix
        /// </summary>
        /// <param name="name">name</param>
        /// <returns>resulting value</returns>
        public static (string Name, string Suffix) SplitManifestAndSuffix(string name)
        {
            int firstUnderscore = name.IndexOf('_');

            return firstUnderscore < 0 ?
                (name, string.Empty) :
                (name.Substring(0, firstUnderscore), name.Substring(firstUnderscore + 1));
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
        ///     Maximums the specified t1
        /// </summary>
        /// <param name="t1">t1</param>
        /// <param name="t2">t2</param>
        /// <returns>resulting value</returns>
        public static TimeSpan? Max(
            TimeSpan? t1,
            TimeSpan? t2)
        {
            if (t1 == null)
            {
                return t2;
            }

            if (t2 == null)
            {
                return t1.Value;
            }

            return t1.Value > t2.Value ? t1 : t2;
        }
    }
}
