// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos
{
    /// <summary>
    ///     CosmosFileSystemUtility class
    /// </summary>
    public class CosmosFileSystemUtility
    {
        /// <summary>
        ///     Trims the and remove trailing slashes
        /// </summary>
        /// <param name="path">path</param>
        /// <returns>resulting value</returns>
        public static string TrimAndRemoveTrailingSlashes(string path)
        {
            if (path != null)
            {
                int end;

                path = path.Trim();

                for (end = path.Length - 1; end >= 0 && path[end] == '/'; --end)
                {
                }

                if (end < path.Length - 1)
                {
                    // need to add 1 because end is the 0 based index and not a 1 based length
                    path = path.Substring(0, end + 1);
                }
            }

            return path;
        }

        /// <summary>
        ///     Splits the name and path
        /// </summary>
        /// <param name="path">path</param>
        /// <returns>resulting value</returns>
        public static (string Path, string Name) SplitNameAndPath(string path)
        {
            int lastSlash = path.LastIndexOf('/');

            return lastSlash < 0 ?
                ("/", path) :
                (path.Substring(0, lastSlash), path.Substring(lastSlash + 1));
        }
    }
}
