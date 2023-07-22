// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using VcClient;

    /// <summary>
    ///     ScanForDuplicateCommandIds class
    /// </summary>
    public class ScanForDuplicateCommandIds : CommandRunner
    {
        public const string CommandName = "dupescan";

        public static IReadOnlyDictionary<string, ICollection<string>> DefaultCache =
            new ReadOnlyDictionary<string, ICollection<string>>(
                new Dictionary<string, ICollection<string>>(StringComparer.InvariantCultureIgnoreCase));
        
        public override IReadOnlyDictionary<string, ICollection<string>> Defaults => ScanForDuplicateCommandIds.DefaultCache;
        
        /// <summary>
        ///     Runs the command
        /// </summary>
        /// <param name="args">arguments</param>
        /// <returns>resulting value</returns>
        public override async Task<ICollection<string>> RunAsync(Parameters args)
        {
            IDictionary<string, ICollection<string>> rawSet =
                new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);

            IDictionary<string, ICollection<string>> multipleOnlySet =
                new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);

            string root = args.Args["path"].First();

            async Task ProcessFileAsync(StreamInfo info)
            {
                string name = Utility.SplitNameAndPath(info.StreamName).Name;

                if (name.StartsWith("RequestManifest"))
                {
                    MemoryStream local = new MemoryStream();
                    HashSet<string> fileCmds = new HashSet<string>();

                    using (Stream remote = VC.ReadStream(info.StreamName, true))
                    {
                        await remote.CopyToAsync(local).ConfigureAwait(false);
                    }

                    local.Seek(0, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(local))
                    {
                        string cmd = sr.ReadLine();

                        while (cmd != null)
                        {
                            if (fileCmds.Contains(cmd) == false)
                            {
                                ICollection<string> files;

                                if (rawSet.TryGetValue(cmd, out files) == false)
                                {
                                    rawSet[cmd] = files = new List<string>();
                                }

                                fileCmds.Add(cmd);
                                files.Add(name);
                            }

                            cmd = sr.ReadLine();
                        }
                    }
                }
            }

            await Utility.EnumerateCosmosDirectoryAsync(root, fileProcessor: ProcessFileAsync);

            foreach (KeyValuePair<string, ICollection<string>> kvp in rawSet.Where(o => o.Value?.Count > 1))
            {
                multipleOnlySet[kvp.Key] = kvp.Value;
            }

            return multipleOnlySet.Select(o => o.Key + "\t" + string.Join(",", o.Value) + "\n").ToList();
        }
    }
}
