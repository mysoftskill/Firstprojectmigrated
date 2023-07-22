// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    internal class VortexEventProcessingResults
    {
        public IList<DeleteRequest> PcfDeleteRequests { get; set; }
    }

    /// <summary>
    ///     Processes vortex events
    /// </summary>
    internal class VortexEventProcessor
    {
        private readonly VortexRequestInformation info;

        private readonly ILogger logger;

        /// <summary>
        ///     The msa identity service adapter
        /// </summary>
        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;

        /// <summary>
        ///     The policy instance to get information from
        /// </summary>
        private readonly Policy policy;

        /// <summary>
        ///     The request guid of this event
        /// </summary>
        private readonly Guid requestGuid;

        /// <summary>
        ///     The vortexEvent to process from vortex.
        /// </summary>
        private readonly VortexEvent vortexEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VortexEventProcessor" /> class.
        /// </summary>
        /// <param name="evt">The vortexEvent to process.</param>
        /// <param name="info">Additional request information</param>
        /// <param name="policy"><see cref="Policy" /> instance to use</param>
        /// <param name="requestGuid">The guid representing the vortex event as a single request</param>
        /// <param name="msaIdentityServiceAdapter">The msa identity service adapter.</param>
        /// <param name="logger">The logger instance.</param>
        public VortexEventProcessor(
            VortexEvent evt,
            VortexRequestInformation info,
            Policy policy,
            Guid requestGuid,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            ILogger logger)
        {
            // Assign a new correlation vector if event doesn't contain one
            string correlationVector = evt.CorrelationVector ?? evt.Tags?.CorrelationVector;
            if (string.IsNullOrWhiteSpace(correlationVector))
            {
                Sll.Context.Vector = new CorrelationVector();
                correlationVector = Sll.Context.Vector.ToString();
            }

            evt.CorrelationVector = correlationVector;

            this.requestGuid = requestGuid;

            this.vortexEvent = evt;
            this.policy = policy;

            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter ?? throw new ArgumentNullException(nameof(msaIdentityServiceAdapter));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.info = info ?? throw new ArgumentNullException(nameof(info));
        }

        /// <summary>
        ///     Processes the vortexEvent async.
        /// </summary>
        /// <returns>A task</returns>
        public async Task<ServiceResponse<VortexEventProcessingResults>> ProcessAsync()
        {
            try
            {
                SignalData[] signals =
                {
                    new SignalData(this.policy.DataTypes.Ids.DeviceConnectivityAndConfiguration.Value, this.requestGuid),
                    new SignalData(this.policy.DataTypes.Ids.ProductAndServiceUsage.Value, this.requestGuid),
                    new SignalData(this.policy.DataTypes.Ids.ProductAndServicePerformance.Value, this.requestGuid),
                    new SignalData(this.policy.DataTypes.Ids.SoftwareSetupAndInventory.Value, this.requestGuid),
                    new SignalData(this.policy.DataTypes.Ids.BrowsingHistory.Value, this.requestGuid),
                    new SignalData(this.policy.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value, this.requestGuid)
                };

                // Get verifier tokens for each signal, before sending it to the writers.
                long targetDeviceId = DeviceIdParser.ParseDeviceIdAsInt64(this.vortexEvent.Ext?.Device?.Id ?? this.vortexEvent.LegacyDeviceId);
                AdapterResponse[] responses = await Task.WhenAll(signals.Select(signal => this.AddVerifierAsync(signal, targetDeviceId))).ConfigureAwait(false);

                // Return the first failure if any
                if (responses.Any(r => !r.IsSuccess))
                {
                    AdapterResponse failure = responses.First(t => !t.IsSuccess);
                    this.logger.Error(nameof(VortexEventProcessor), failure.Error.Message);

                    return new ServiceResponse<VortexEventProcessingResults>
                    {
                        Error = new Error(ErrorCode.PartnerError, failure.Error.Message)
                    };
                }

                return new ServiceResponse<VortexEventProcessingResults>
                {
                    Result = new VortexEventProcessingResults
                    {
                        PcfDeleteRequests = signals.Select(
                            sig => PrivacyRequestConverter.CreateVortexDeviceDeleteRequest(
                                this.vortexEvent,
                                this.info.RequestTime,
                                sig, 
                                this.policy)).ToList()
                    }
                };
            }
            catch (DeviceIdFormatException e)
            {
                return new ServiceResponse<VortexEventProcessingResults>
                {
                    Error = new Error(ErrorCode.InvalidInput, e.Message)
                };
            }
            catch (Exception e)
            {
                return new ServiceResponse<VortexEventProcessingResults>
                {
                    Error = new Error(ErrorCode.Unknown, e.Message)
                };
            }
        }

        /// <summary>
        ///     Adds the verifier to the signal data.
        /// </summary>
        /// <param name="signalData">The signal data.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>An <see cref="AdapterResponse" /> indicating success or failure</returns>
        private async Task<AdapterResponse> AddVerifierAsync(SignalData signalData, long deviceId)
        {
            AdapterResponse<string> token = await this.msaIdentityServiceAdapter.GetGdprDeviceDeleteVerifierAsync(signalData.CommandId, deviceId).ConfigureAwait(false);
            if (!token.IsSuccess)
            {
                return token;
            }

            signalData.VerifierToken = token.Result;

            return new AdapterResponse();
        }
    }
}
