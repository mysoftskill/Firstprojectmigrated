// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using VcClient;

    /*
        args = new[]
        {
            "-minHours",
            "24",
            "-root",
            "https://cosmos08.osdinfra.net/cosmos/PXSCosmos08.Prod/local/ExportAndAuditDrop/PROD/v2/ExportData/",
            "-root",
            "https://cosmos09.osdinfra.net/cosmos/PXSCosmos09.Prod/local/ExportAndAuditDrop/PROD/v2/ExportData/",
            "-root",
            "https://cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/ExportAndAuditDrop/PROD/v2/ExportData/",
            "-includeDataFiles",
            "false",
        };
    */

    /// <summary>
    ///     MaxAveBatchAgePerAgent class
    /// </summary>
    public class MaxAveBatchAgePerAgent : CommandRunner
    {
        public const string CommandName = "enum";

        public static IReadOnlyDictionary<string, ICollection<string>> DefaultCache =
            new ReadOnlyDictionary<string, ICollection<string>>(
                new Dictionary<string, ICollection<string>>(StringComparer.InvariantCultureIgnoreCase)
                {
                    {
                        "root",
                        new []
                        {
                            "https://cosmos08.osdinfra.net/cosmos/PXSCosmos08.Prod/local/ExportAndAuditDrop/PROD/v2/ExportData/",
                            "https://cosmos09.osdinfra.net/cosmos/PXSCosmos09.Prod/local/ExportAndAuditDrop/PROD/v2/ExportData/",
                            "https://cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/ExportAndAuditDrop/PROD/v2/ExportData/",
                        }
                    },
                    { "minHours", new [] { "24" } },
                    { "includeDataFiles", new [] { "false" } },
                });

        /// <summary>Gets defaults</summary>
        public override IReadOnlyDictionary<string, ICollection<string>> Defaults => MaxAveBatchAgePerAgent.DefaultCache;

        public override async Task<ICollection<string>> RunAsync(Parameters args)
        {
            ICollection<string> roots = args["root"];
            List<string> result = new List<string>();
            TimeSpan minAge = TimeSpan.FromHours(Convert.ToInt32(args["minhours"].First()));
            bool incDataFiles = bool.Parse(args["includedatafiles"].First());

            foreach (string root in roots)
            {
                await Utility.EnumerateCosmosDirectoryAsync(
                    root, 
                    dirProcessor: si => MaxAveBatchAgePerAgent.ProcessAgentAsync(si, minAge, incDataFiles, result));
            }

            return result;
        }

        public override Task OnComplete()
        {
            Console.WriteLine();

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press enter to continue");
                Console.Read();
            }

            return Task.CompletedTask;
        }

        private static async Task ProcessAgentAsync(
            StreamInfo agentDir, 
            TimeSpan minAge,
            bool incDataFiles,
            ICollection<string> resultSet)
        {
            Dictionary<string, (TimeSpan? Data, TimeSpan? Request)> manifestSet;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            StringBuilder builder = new StringBuilder();
            FileSetStats manifest = new FileSetStats();
            FileSetStats data = new FileSetStats();
            string agent = Utility.SplitNameAndPath(agentDir.StreamName).Name;

            manifestSet = new Dictionary<string, (TimeSpan? Data, TimeSpan? Request)>(StringComparer.OrdinalIgnoreCase);

            Task ProcessFile(StreamInfo file)
            {
                TimeSpan age = now - file.CreateTime;
                string fileName;
                string suffix;
                string prefix;

                fileName = Utility.SplitNameAndPath(file.StreamName).Name;
                (prefix, suffix) = Utility.SplitManifestAndSuffix(fileName);

                if (prefix.StartsWithIgnoreCase("DataFileManifest"))
                {
                    (TimeSpan? Data, TimeSpan? Request) item;

                    if (manifestSet.TryGetValue(suffix, out item) && item.Request.HasValue)
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        TimeSpan max = Utility.Max(item.Request, age).Value;
                        manifest.AddFile(max, 0);

                        item.Data = age;
                    }
                    else
                    {
                        item = (age, null);
                    }

                    manifestSet[suffix] = item;
                }
                else if (prefix.StartsWithIgnoreCase("RequestManifest"))
                {
                    (TimeSpan? Data, TimeSpan? Request) item;

                    if (manifestSet.TryGetValue(suffix, out item) && item.Data.HasValue)
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        TimeSpan max = Utility.Max(item.Data, age).Value;
                        manifest.AddFile(max, 0);

                        item.Request = age;
                    }
                    else
                    {
                        item = (null, age);
                    }

                    manifestSet[suffix] = item;
                }
                else if (incDataFiles)
                {
                    data.AddFile(age, file.Length);
                }
                
                return Task.CompletedTask;
            }

            await Utility.EnumerateCosmosDirectoryAsync(
                agentDir.StreamName,
                fileProcessor: ProcessFile);

            if ((manifest.Count > 0 && manifest.AgeMax > minAge) || 
                (data.Count > 0 && data.AgeMax > minAge))
            {
                builder.Append(agent);
                manifest.WriteTo(builder, false);

                if (incDataFiles)
                {
                    data.WriteTo(builder, true);
                }

                builder.Append('\n');

                resultSet.Add(builder.ToString());
            }
        }

        private class FileSetStats
        {
            private long ticksTotal;
            private long ticksMax;

            private long sizeTotal;
            private long sizeMax;

            public TimeSpan AgeMax => new TimeSpan(this.ticksMax);

            public int Count { get; private set; }

            public void AddFile(
                TimeSpan age,
                long size)
            {
                this.ticksTotal += age.Ticks;
                this.ticksMax = Math.Max(this.ticksMax, age.Ticks);

                this.sizeTotal += size;
                this.sizeMax = Math.Max(this.sizeMax, size);

                this.Count += 1;
            }

            public void WriteTo(
                StringBuilder builder,
                bool emitSize)
            {
                builder.Append('\t');
                builder.Append(this.Count.ToString());
                builder.Append('\t');
                builder.Append(Convert.ToInt64(new TimeSpan(this.ticksMax).TotalHours));
                builder.Append('\t');
                builder.Append(
                    this.Count > 0 ?
                        Convert.ToInt64(new TimeSpan(this.ticksTotal / this.Count).TotalHours) :
                        0);

                if (emitSize)
                {
                    builder.Append('\t');
                    builder.Append(this.sizeMax.ToString("n0"));
                    builder.Append('\t');
                    builder.Append(this.Count > 0 ? (this.sizeTotal / this.Count).ToString("n0") : "0");
                }
            }
        }
    }
}
