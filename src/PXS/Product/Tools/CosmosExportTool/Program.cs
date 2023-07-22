// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    ///     Program class
    /// </summary>
    public class Program
    {
        private static readonly Dictionary<string, Func<ICommandRunner>> Commands =
            new Dictionary<string, Func<ICommandRunner>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { AlterCosmosExportFilesSuffix.CommandName, () => new AlterCosmosExportFilesSuffix() },
                { ScanForDuplicateCommandIds.CommandName, () => new ScanForDuplicateCommandIds() },
                { MaxAveBatchAgePerAgent.CommandName, () => new MaxAveBatchAgePerAgent() },
            };

        /// <summary>
        ///     application entry point
        /// </summary>
        /// <param name="args">arguments</param>
        public static void Main(string[] args)
        {
            new Program().RunAsync(args).Wait();
        }

        /// <summary>
        ///     application entry point
        /// </summary>
        /// <param name="args">arguments</param>
        /// <returns>resulting value</returns>
        private async Task RunAsync(string[] args)
        {
            ICommandRunner runner = null;
            string name = "*MISSING*";

            if (args?.Length > 0)
            {
                name = args[0];
                if (Program.Commands.TryGetValue(name, out Func<ICommandRunner> generator))
                {
                    runner = generator();
                }
            }

            if (runner != null)
            {
                Parameters argObj = Parameters.Parse(runner, args.Skip(1).ToList());

                try
                {
                    foreach (string s in await runner.RunAsync(argObj))
                    {
                        Console.Write(s);
                    }
                }
                catch (ConfigException e)
                {
                    Console.WriteLine("Configuration parse error: " + e.Message);
                    return;
                }

                await runner.OnComplete();
            }
            else
            {
                Console.WriteLine(
                    $"Missing or unknown command [{name}]. Valid commands are: {string.Join(",", Program.Commands.Keys)}");
            }
        }
    }
}
