// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.AadAccountCloseDeadLetterRestorer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.PrivacyHost;

    using Microsoft.PrivacyServices.Common.Azure;

    internal class Program
    {
        private const string ComponentName = "AadAccountCloseDeadLetterRestorer";

        public static void Main(string[] args)
        {
            IAadAccountCloseWorkerConfiguration config = null;
            try
            {
                // Setup dependencies
                ResetTraceDecorator.ResetTraceListeners();
                ResetTraceDecorator.AddConsoleTraceListener();
                ILogger logger = new ConsoleLogger();
                ICounterFactory counterFactory = new NoOpCounterFactory();


                IPrivacyConfigurationManager configurationManager = null;
                try
                {
                    configurationManager = PrivacyConfigurationManager.LoadCurrentConfiguration(logger);
                    config = configurationManager.AadAccountCloseWorkerConfiguration;
                }
                catch (Exception e)
                {
                    logger.Error(ComponentName, e, "Could not load configuration.");
                }

                IList<IAzureStorageProvider> queueStorageProviders = new List<IAzureStorageProvider>();

                foreach (IAzureStorageConfiguration storageConfiguration in config.QueueProccessorConfig.AzureQueueStorageConfigurations)
                {
                    AzureStorageProvider storage = new AzureStorageProvider(logger, new AzureKeyVaultReader(configurationManager, new Clock(), logger));
                    storage.InitializeAsync(storageConfiguration).GetAwaiter().GetResult();
                    queueStorageProviders.Add(storage);
                }

                if (queueStorageProviders.Count == 0)
                {
                    Trace.TraceError("No queue storage providers found. Cannot process anything. Check configuration.");
                    return;
                }

                ITable<AccountCloseDeadLetterStorage> deadLetterTable = new AzureTable<AccountCloseDeadLetterStorage>(
                    queueStorageProviders[0],
                    logger,
                    nameof(AccountCloseDeadLetterStorage).ToLowerInvariant());

                // Read configuration file for commands that need attempted
                IList<AadAccount> commands = LoadAadAccountsFromConfigurationFile();

                if (commands == null || commands.Count == 0)
                {
                    Trace.TraceError("No account close events found in configuration file.");
                    return;
                }

                // Grab commands from dead letter
                var deadLetterReader = new AadDeadLetterReader(deadLetterTable, logger);
                IList<AccountCloseDeadLetterStorage> deadLetterAccounts = deadLetterReader.ReadAsync(commands).GetAwaiter().GetResult();

                if (deadLetterAccounts == null || deadLetterAccounts.Count == 0)
                {
                    Trace.TraceError("No account close events found in dead letter matching the input object id + tenant id's provided.");
                }
                else
                {
                    logger.Information(ComponentName, "Starting enqueue of commands.");

                    // Enqueue them so worker picks up for processing
                    IAccountCloseQueueManager accountCloseQueueManager = new AadAccountCloseQueueManager(
                        queueStorageProviders,
                        logger,
                        config.QueueProccessorConfig,
                        counterFactory);
                    accountCloseQueueManager.EnqueueAsync(deadLetterAccounts.Select(c => c.DataActual).ToList(), CancellationToken.None).GetAwaiter().GetResult();

                    Trace.TraceInformation($"Success! Finished enqueueing commands for re-processing. Total: {deadLetterAccounts.Count}");
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
            finally
            {
#if DEBUG
                if (config != null && config.IsRunningLocally)
                {
                    Console.WriteLine("Press any key to continue..");
                    Console.ReadLine();
                }
#endif

                Trace.Flush();
            }
        }

        private static IList<AadAccount> LoadAadAccountsFromConfigurationFile()
        {
            IList<AadAccount> aadAccounts = new List<AadAccount>();

            using (StreamReader sr = new StreamReader("input.txt"))
            {
                string line;
                int index = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    index++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        Trace.TraceWarning($"#{index}: Ignoring emtpy line.");
                        continue;
                    }

                    if (line.StartsWith(";"))
                    {
                        Trace.TraceWarning($"#{index}: Ignoring line that starts with ;");
                        continue;
                    }

                    try
                    {
                        string[] lineSplit = line.Split(',');
                        if (lineSplit.Length < 2)
                        {
                            Trace.TraceWarning($"#{index}: Ignoring line due to incorrect line format.");
                        }
                        else
                        {
                            var account = new AadAccount(Guid.Parse(lineSplit[0].TrimStart(' ').TrimEnd(' ')), Guid.Parse(lineSplit[1].TrimStart(' ').TrimEnd(' ')));
                            aadAccounts.Add(account);
                            Trace.TraceInformation($"#{index}: Successfully added {account},  to queue.");
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"#{index}: Could not read line due to exception: {e}");
                    }
                }
            }

            return aadAccounts;
        }
    }
}
