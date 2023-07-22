// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;

    /// <summary>
    ///     RequestManifestReader class
    /// </summary>
    public static class RequestManifestReader
    {
        /// <summary>
        ///     Gets the list of command ids in the request manifest file
        /// </summary>
        /// <param name="manifest">request manifest</param>
        /// <param name="refresher">function to exectute after processing every refreshRowFreq rows</param>
        /// <param name="refreshRowFreq">frequency (in row count) to execute the refresher function</param>
        /// <param name="errorLogger">error logger</param>
        /// <returns>list of commands in the manfiest</returns>
        public static async Task<(ICollection<string>, long rowCount)> ExtractCommandIdsFromManifestFileAsync(
            IFile manifest,
            Func<Task> refresher,
            int refreshRowFreq,
            Action<string> errorLogger)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Stream contents;
            long rowCount = 0;

            // next, fetch the data manifest file and build a list of all command files
            try
            {
                contents = manifest.GetDataReader();
            }
            catch (FileNotFoundException)
            {
                return (null, 0);
            }

            using (StreamReader sr = new StreamReader(contents))
            {
                int rowsSinceLastRefresh = 1;

                for (;;)
                {
                    string current = await sr.ReadLineAsync().ConfigureAwait(false);
                    if (current == null)
                    {
                        break;
                    }

                    try
                    {
                        string[] columns = current.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (columns.Length >= 1)
                        {
                            // note that becuase we use a HashSet, we'll get a set of distinct command ids in the end
                            result.Add(Utility.CanonicalizeCommandId(columns[0].Trim()));
                            continue;
                        }

                        // We'll fail to mark complete some jobs becuase of this, but can still continue and mark complete those 
                        //  that we can

                        errorLogger(
                            $"Failed to process row {rowCount} in request manifest file {manifest.Name}. Row is [{current}]");
                    }
                    finally
                    {
                        ++rowCount;

                        if (refresher != null)
                        {
                            if (rowsSinceLastRefresh < refreshRowFreq)
                            {
                                ++rowsSinceLastRefresh;
                            }
                            else
                            {
                                await refresher().ConfigureAwait(false);
                                rowsSinceLastRefresh = 1;
                            }
                        }
                    }
                }
            }

            return (result, rowCount);
        }
    }
}
