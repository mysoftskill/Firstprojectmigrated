// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using VcClient;

    /*
        args = new[]
        {
            "-root",
            "https://cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/ExportAndAuditDrop/PROD/v2/ExportData/",
            "-agentId",
            "23728d96-e32b-4401-8c12-93b5a3a975c7",
            "-source",
            "2018_06_06_00",
            "-new",
            "2017_06_06_00",
            "-skipDataFiles",
            "false"
            "-force",
            "false"
        };
     */

    /// <summary>
    ///     RenameDroppedFilesSuffix class
    /// </summary>
    public class AlterCosmosExportFilesSuffix : CommandRunner
    {
        public const string CommandName = "rename";

        public static IReadOnlyDictionary<string, ICollection<string>> DefaultCache =
            new ReadOnlyDictionary<string, ICollection<string>>(
                new Dictionary<string, ICollection<string>>(StringComparer.InvariantCultureIgnoreCase)
                {
                    {
                        "root",
                        new[]
                        {
                            "https://cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/ExportAndAuditDrop/PROD/v2/ExportData/"
                        }
                    },
                    { "skipDataFiles", new[] { "false" } },
                    { "force", new[] { "false" } },
                });

        public override IReadOnlyDictionary<string, ICollection<string>> Defaults => AlterCosmosExportFilesSuffix.DefaultCache;

        public override async Task<ICollection<string>> RunAsync(Parameters args)
        {
            ICollection<(string Src, string Dest)> renames = new List<(string, string)>();
            List<string> result = new List<string>();
            string sourceSuffix = args["source"].First();
            string newSuffix = args["new"].First();
            string agentId = Utility.EnsureHasTrailingSlashButNoLeadingSlash(args["agentid"].First());
            string root = Utility.EnsureTrailingSlash(args["root"].First()) + agentId;
            bool skipDataFiles = bool.Parse(args["skipdatafiles"].First());
            bool simulate = true;

            void DoWork(IEnumerable<(string Src, string Dest)> files)
            {
                foreach ((string Src, string Dest) item in files)
                {
                    if (simulate)
                    {
                        result.Add($"Simulating rename of {item.Src} to {item.Dest}\n");
                        continue;
                    }

                    try
                    {
                        VC.Rename(item.Src, item.Dest);
                        result.Add($"Successfully renamed {item.Src} to {item.Dest}\n");
                    }
                    catch (Exception e)
                    {
                        result.Add($"Failed to rename {item.Src} to {item.Dest}: {e}\n");
                    }
                }
            }

            if (args.Args.ContainsKey("force"))
            {
                simulate = bool.Parse(args["force"].First()) == false;
            }

            await Utility.EnumerateCosmosDirectoryAsync(
                root,
                filter: si => si.StreamName.Contains(sourceSuffix) == false,
                fileProcessor: si => 
                    AlterCosmosExportFilesSuffix.ProcessFileAsync(si, sourceSuffix, newSuffix, renames, result));

            if (skipDataFiles == false)
            {
                DoWork(
                    renames
                        .Where(
                            o =>
                                o.Src.IndexOf("DataFileManifest", StringComparison.OrdinalIgnoreCase) < 0 &&
                                o.Src.IndexOf("RequestManifest", StringComparison.OrdinalIgnoreCase) < 0));
            }

            DoWork(
                renames
                    .Where(
                        o =>
                            o.Src.IndexOf("DataFileManifest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            o.Src.IndexOf("RequestManifest", StringComparison.OrdinalIgnoreCase) >= 0));


            return result;
        }

        private static Task ProcessFileAsync(
            StreamInfo streamInfo,
            string original,
            string desired,
            ICollection<(string src, string dest)> renames,
            ICollection<string> result)
        {
            string newPath;
            string path;
            string name;

            (path, name) = Utility.SplitNameAndPath(streamInfo.StreamName);

            newPath = Utility.EnsureTrailingSlash(path) + name.Replace(original, desired);

            if (newPath.EqualsIgnoreCase(streamInfo.StreamName))
            {
                result.Add("No change for " + newPath + "\n");
            }
            else
            {
                renames.Add((streamInfo.StreamName, newPath));
            }

            return Task.CompletedTask;
        }
    }
}
