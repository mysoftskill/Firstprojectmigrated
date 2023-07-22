// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierWorker
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// PreVerifierWorker
    /// </summary>
    public class PreVerifierWorker : BaseRecurringDeleteQueueWorker
    {
        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;

        public PreVerifierWorker(
            ICloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue,
            ICloudQueueConfiguration cloudQueueConfiguration,
            IScheduleDbConfiguration scheduleDbConfiguration,
            IAppConfiguration appConfiguration,
            IScheduleDbClient scheduleDbClient,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            ILogger logger) : base(cloudQueue, cloudQueueConfiguration, scheduleDbConfiguration, appConfiguration, scheduleDbClient, logger)
        {
            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter ?? throw new ArgumentNullException(nameof(msaIdentityServiceAdapter));
        }

        public override async Task ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(
            RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument,
            OutgoingApiEventWrapper outgoingApi)
        {
            if (recurrentDeleteScheduleDbDocument == null)
            {
                throw new ArgumentNullException(nameof(recurrentDeleteScheduleDbDocument));
            }

            var preVerifier = recurrentDeleteScheduleDbDocument.PreVerifier;

            if (string.IsNullOrEmpty(preVerifier))
            {
                throw new ArgumentNullException("The existing PreVerifier is null or empty.");
            }

            var requestContext = new PxfRequestContext(
                   userProxyTicket: null,
                   familyJsonWebToken: null,
                   authorizingPuid: 0,
                   targetPuid: 0,
                   targetCid: null,
                   countryRegion: null,
                   isWatchdogRequest: false,
                   flights: null);

            try
            {
                // Renew pre-verifier using existing pre-verifier
                var preVerifierResponse = await this.msaIdentityServiceAdapter
                                .RenewGdprUserDeleteVerifierUsingPreverifierAsync(requestContext, preVerifier).ConfigureAwait(false);
                if (!preVerifierResponse.IsSuccess)
                {
                    this.logger.Error(this.componentName, $"Fail to renew pre-verifier: ErrorCode={preVerifierResponse.Error.Code}, ErrorMessage={preVerifierResponse.Error.Message}");
                    throw new MsaRvsApiException(nameof(ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync) + $"Fail to renew pre-verifier: AdapterErrorCode={preVerifierResponse.Error.Code}, AdapterErrorMessage={preVerifierResponse.Error.Message}");
                }

                var renewedVerifier = preVerifierResponse.Result;
                var renewedVerifierExpirationTimeUtc = MsaIdentityServiceAdapter.GetExpiryTimeFromVerifier(renewedVerifier);

                // The schedule DB document could be modified before the pre-verifier worker is able to renew
                // the pre-verifier. If this does happen, we'll get the latest document, set the pre-verifier and 
                // its expiration time and write to schedule DB again.
                await this.UpdateScheduleDbAsync(recurrentDeleteScheduleDbDocument,
                    (doc) =>
                    {
                        doc.PreVerifier = renewedVerifier;
                        doc.PreVerifierExpirationDateUtc = renewedVerifierExpirationTimeUtc;
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!(ex is MsaRvsApiException))
                {
                    this.logger.Error(this.componentName, ex, $"Exception ocurred while processing RecurrentDeleteScheduleDbDocument: {ex.Message}");
                }
                await this.UpdateScheduleDbAsync(recurrentDeleteScheduleDbDocument,
                    doc =>
                    {
                        if (doc.NumberOfRetries >= this.scheduleDbConfig.MaxNumberOfRetries)
                        {
                            doc.RecurrentDeleteStatus = RecurrentDeleteStatus.Paused;
                        }
                        else
                        {
                            doc.NumberOfRetries++;
                        }
                    }).ConfigureAwait(false);
                throw;
            }
        }
    }
}

