// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     This processes UserDelete <see cref="CDPEvent2" /> events by transforming them into <see cref="AccountDeleteInformation"/>
    /// </summary>
    public class UserDeleteEventProcessorV2 : IUserDeleteEventProcessor
    {
        private readonly IClock clock;

        private readonly ICounterFactory counterFactory;

        private readonly CdpEvent2Helper eventHelper;

        private readonly ILogger logger;

        /// <inheritdoc />
        public CdpEvent2Helper EventHelper => this.eventHelper;

        public UserDeleteEventProcessorV2(CdpEvent2Helper eventHelper, IClock clock, ICounterFactory counterFactory, ILogger logger)
        {
            this.eventHelper = eventHelper ?? throw new ArgumentNullException(nameof(eventHelper));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<AccountDeleteInformation>> ProcessDeleteItemAsync(CDPEvent2 evt, CancellationToken token = default)
        {
            var response = await this.ProcessDeleteItemsAsync(new[] { evt }, token).ConfigureAwait(false);
            if (!response.IsSuccess)
            {
                return new AdapterResponse<AccountDeleteInformation>
                {
                    Error = response.Error
                };
            }

            // Using .First() instead of FirstOrDefault since we always expect something back by this point.
            return response.Result.First();
        }

        /// <summary>
        ///     Process delete items by writing to an Azure Queue
        /// </summary>
        /// <param name="eventCollection">The collection of <see cref="CDPEvent2" /> to process.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<AdapterResponse<IList<AdapterResponse<AccountDeleteInformation>>>> ProcessDeleteItemsAsync(
            IEnumerable<CDPEvent2> eventCollection,
            CancellationToken token = default)
        {
            List<CDPEvent2> events = eventCollection.ToList();
            if (!events.Any())
            {
                var response = new AdapterResponse<IList<AdapterResponse<AccountDeleteInformation>>>
                {
                    Result = new List<AdapterResponse<AccountDeleteInformation>>()
                };

                return Task.FromResult(response);
            }

            // Transform CDP Events in to AccountDeleteInformation
            List<AdapterResponse<AccountDeleteInformation>> deleteInfos = events.Select(
                evt =>
                {
                    token.ThrowIfCancellationRequested();

                    var adi = new AccountDeleteInformation();
                    var response = new AdapterResponse<AccountDeleteInformation>
                    {
                        Result = adi
                    };

                    adi.Puid = long.Parse(evt.AggregationKey, NumberStyles.AllowHexSpecifier);

                    if (!this.EventHelper.TryGetCid(evt, out long cid))
                    {
                        this.IncrementProcessingFailure("missingcid");
                        response.Error = new AdapterError(AdapterErrorCode.EmptyResponse, $"{nameof(CDPEvent2)} is missing CID.", 500);
                        return response;
                    }

                    adi.Cid = cid;

                    if (!this.EventHelper.TryGetGdprPreVerifierToken(evt, out string preVerifier))
                    {
                        this.IncrementProcessingFailure("missingpreverifier");
                        response.Error = response.Error ?? new AdapterError(AdapterErrorCode.EmptyResponse, "Event did not container a GDPR per-verifier token", 500);
                        return response;
                    }

                    adi.PreVerifierToken = preVerifier;

                    adi.Reason = this.EventHelper.GetDeleteReason(evt);
                    MsaAgeOutEventValidation.ProcessMsaAgeOutEvent(adi, evt, this.EventHelper, this.clock, this.logger, this.counterFactory);

                    if (response.IsSuccess)
                    {
                        this.IncrementProcessingSuccess();
                    }

                    return response;
                }).ToList();

            var retVal = new AdapterResponse<IList<AdapterResponse<AccountDeleteInformation>>>
            {
                Result = deleteInfos
            };

            return Task.FromResult(retVal);
        }

        /// <summary>
        ///     Increments the processing failure count by one
        /// </summary>
        /// <param name="errorId">String identifying the error</param>
        private void IncrementProcessingFailure(string errorId)
        {
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.MsaAccountClose, "failure", CounterType.Rate);
            counter.Increment();
            counter.Increment(errorId);
        }

        /// <summary>
        ///     Increments the processing success by one
        /// </summary>
        private void IncrementProcessingSuccess()
        {
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.MsaAccountClose, "success", CounterType.Rate);
            counter.Increment();
        }
    }
}
