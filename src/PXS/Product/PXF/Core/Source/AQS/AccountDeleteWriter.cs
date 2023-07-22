// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AQS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <inheritdoc />
    /// <summary>
    ///     Wrapper on when we want to write deletes to multiple places
    /// </summary>
    public class AccountDeleteWriter : IAccountDeleteWriter
    {
        private readonly ICounterFactory counterFactory;

        private readonly ILogger logger;

        private readonly IPcfAdapter pcfAdapter;

        public AccountDeleteWriter(
            IPcfAdapter pcfAdapter,
            ICounterFactory counterFactory,
            ILogger logger)
        {
            this.pcfAdapter = pcfAdapter ?? throw new ArgumentNullException(nameof(pcfAdapter));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        /// <summary>
        ///     Writes out the <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.AccountDeleteInformation" /> to all registered writers
        /// </summary>
        /// <param name="deleteInfo"> The <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.AccountDeleteInformation" /> to write out </param>
        /// <param name="requester"> The requestor </param>
        public async Task<AdapterResponse<AccountDeleteInformation>> WriteDeleteAsync(AccountDeleteInformation deleteInfo, string requester)
        {
            AdapterResponse<IList<AccountDeleteInformation>> response =
                await this.WriteDeletesAsync(new List<AccountDeleteInformation> { deleteInfo }, requester).ConfigureAwait(false);
            if (!response.IsSuccess)
            {
                return new AdapterResponse<AccountDeleteInformation>
                {
                    Error = response.Error
                };
            }

            return new AdapterResponse<AccountDeleteInformation>
            {
                Result = response.Result.FirstOrDefault()
            };
        }

        /// <inheritdoc />
        /// <summary>
        ///     Writes out a bulk delete of <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.AccountDeleteInformation" />
        /// </summary>
        /// <param name="accountDeleteInfos">The collection of delete information</param>
        /// <param name="requester">The requester ID of who is issuing the delete</param>
        /// <returns>
        ///     A collection of the requests IDs
        /// </returns>
        public async Task<AdapterResponse<IList<AccountDeleteInformation>>> WriteDeletesAsync(IList<AccountDeleteInformation> accountDeleteInfos, string requester)
        {
            if (accountDeleteInfos.Count == 0)
            {
                return new AdapterResponse<IList<AccountDeleteInformation>>();
            }

            // Must successfully send events to PCF
            List<PrivacyRequest> requests = accountDeleteInfos.Select(adi => adi.ToAccountCloseRequest(requester)).Cast<PrivacyRequest>().ToList();
            AdapterResponse pcfAdapterResult = await this.pcfAdapter.PostCommandsAsync(requests).ConfigureAwait(false);
            if (!pcfAdapterResult.IsSuccess)
            {
                return new AdapterResponse<IList<AccountDeleteInformation>>
                {
                    Error = pcfAdapterResult.Error
                };
            }

            // Update stats on what was sent to partners
            this.UpdateCloseStatistics(accountDeleteInfos);

            return new AdapterResponse<IList<AccountDeleteInformation>>
            {
                Result = accountDeleteInfos.ToList()
            };
        }

        private void UpdateCloseStatistics(IList<AccountDeleteInformation> accountDeleteInfos)
        {
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.MsaAccountClose, "accountstatistics", CounterType.Rate);
            counter.IncrementBy((ulong)accountDeleteInfos.Count);
            counter.IncrementBy((ulong)accountDeleteInfos.Count(info => !string.IsNullOrEmpty(info.Xuid) && !string.Equals(default(int).ToString(), info.Xuid)), "HasXuid");
            counter.IncrementBy((ulong)accountDeleteInfos.Count(info => info.Reason == AccountCloseReason.UserAccountAgedOut), "UserAccountAgedOut");
            counter.IncrementBy((ulong)accountDeleteInfos.Count(info => info.Reason == AccountCloseReason.UserAccountClosed), "UserAccountClosed");
            counter.IncrementBy((ulong)accountDeleteInfos.Count(info => info.Reason == AccountCloseReason.UserAccountCreationFailure), "UserAccountCreationFailed");
            counter.IncrementBy((ulong)accountDeleteInfos.Count(info => info.Reason == AccountCloseReason.None), "MissingCloseReason");
            counter.IncrementBy((ulong)accountDeleteInfos.Count(info => info.Reason == AccountCloseReason.Test), "TestClose");
        }
    }
}
