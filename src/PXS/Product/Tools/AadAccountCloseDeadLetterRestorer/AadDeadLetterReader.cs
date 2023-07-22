// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.AadAccountCloseDeadLetterRestorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     AadDeadLetterReader
    /// </summary>
    internal class AadDeadLetterReader
    {
        private readonly ILogger logger;

        private readonly ITable<AccountCloseDeadLetterStorage> table;

        public AadDeadLetterReader(ITable<AccountCloseDeadLetterStorage> deadLetterTable, ILogger logger)
        {
            this.table = deadLetterTable;
            this.logger = logger;
        }

        internal async Task<IList<AccountCloseDeadLetterStorage>> ReadAsync(IList<AadAccount> aadAccounts)
        {
            IList<AccountCloseDeadLetterStorage> accountsFromStorage = new List<AccountCloseDeadLetterStorage>();
            foreach (AadAccount account in aadAccounts)
            {
                try
                {
                    AccountCloseDeadLetterStorage storageItem = await this.table.GetItemAsync(account.TenantId.ToString(), account.ObjectId.ToString()).ConfigureAwait(false);

                    if (storageItem != null)
                    {
                        accountsFromStorage.Add(storageItem);
                    }
                    else
                    {
                        this.logger.Warning(
                            nameof(AadDeadLetterReader),
                            $"Ignoring this request. Account was not found in dead-letter storage: {account}.");
                    }
                }
                catch (Exception e)
                {
                    this.logger.Error(
                        nameof(AadDeadLetterReader),
                        e,
                        $"Exception when reading from storage. Could not retrieve account: {account}");
                }
            }

            return accountsFromStorage;
        }
    }
}
