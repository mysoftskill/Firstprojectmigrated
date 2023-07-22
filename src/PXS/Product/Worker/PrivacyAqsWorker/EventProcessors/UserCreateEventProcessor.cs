// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;

    /// <summary>
    ///     Processes <see cref="CDPEvent2" />s containing <see cref="UserDelete" /> <see cref="EventData" />
    /// </summary>
    public class UserCreateEventProcessor : IUserCreateEventProcessor
    {
        private readonly ICounterFactory counterFactory;

        /// <summary>
        ///     MSA service adapter for getting user account information
        /// </summary>
        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;

        /// <summary>
        ///     Initializes a new instance of <see cref="UserCreateEventProcessor" />.
        /// </summary>
        /// <param name="msaIdentityServiceAdapter">Service adapter to MSA for pulling newly created account information</param>
        /// <param name="counterFactory">Counter Factory for making performance counters</param>
        public UserCreateEventProcessor(IMsaIdentityServiceAdapter msaIdentityServiceAdapter, ICounterFactory counterFactory)
        {
            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter;
            this.counterFactory = counterFactory;
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<IList<AccountCreateInformation>>> ProcessCreateItemsAsync(IEnumerable<CDPEvent2> createItems)
        {
            List<long> puids = createItems.Select(item => long.Parse(item.AggregationKey, NumberStyles.AllowHexSpecifier)).ToList();
            AdapterResponse<IEnumerable<ISigninNameInformation>> infoResponse = await this.msaIdentityServiceAdapter.GetSigninNameInformationsAsync(puids).ConfigureAwait(false);
            if (!infoResponse.IsSuccess)
            {
                this.IncrementProcessingFailure((uint)puids.Count, "getsigninnames");
                return new AdapterResponse<IList<AccountCreateInformation>>
                {
                    Error = infoResponse.Error
                };
            }

            List<AccountCreateInformation> result = infoResponse.Result?.Where(info => info.Puid != null && info.Cid != null).Select(
                                                        info => new AccountCreateInformation
                                                        {
                                                            Puid = (ulong)info.Puid,
                                                            Cid = (long)info.Cid
                                                        }).ToList() ?? new List<AccountCreateInformation>();

            if (puids.Count > result.Count)
            {
                this.IncrementProcessingFailure((uint)(puids.Count - result.Count), "missingcid");
            }

            this.IncrementProcessingSuccess((uint)result.Count);

            return new AdapterResponse<IList<AccountCreateInformation>>
            {
                Result = result
            };
        }

        /// <summary>
        ///     Increments the processing failure counter
        /// </summary>
        /// <param name="count">Number of items that were affected</param>
        /// <param name="errorId">Error string for identifying why the failure happened</param>
        private void IncrementProcessingFailure(uint count, string errorId)
        {
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.MsaAccountCreate, "failure", CounterType.Rate);
            counter.IncrementBy(count);
            counter.IncrementBy(count, errorId);
        }

        /// <summary>
        ///     Increments the processing success counter
        /// </summary>
        /// <param name="count">The number of items that were processed successfully</param>
        private void IncrementProcessingSuccess(uint count)
        {
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.MsaAccountCreate, "success", CounterType.Rate);
            counter.IncrementBy(count);
        }
    }
}
