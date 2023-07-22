// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using static Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.AadRequestVerificationServiceAdapter;

    /// <summary>
    ///     AadAccountCloseService
    /// </summary>
    public class AadAccountCloseService : IAadAccountCloseService
    {
        private readonly IAadRequestVerificationServiceAdapter aadRvsAdapter;

        private readonly ILogger logger;

        private readonly IPcfAdapter pcfAdapter;

        private readonly IVerificationTokenValidationService verificationTokenValidationService;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Creates a new instance of AadAccountCloseService
        /// </summary>
        /// <param name="pcfAdapter">The pcf adapter. Used to communicate with PCF.</param>
        /// <param name="verificationTokenValidationService">The verification token validation serive. Used to validate verifiers are valid.</param>
        /// <param name="aadRvsAdapter">The aad rvs adapter. Used to communicate with AAD RVS.</param>
        /// <param name="logger">The logger</param>
        /// <param name="appConfiguration">The IAppConfiguration instance for FeatureFlag</param>
        public AadAccountCloseService(
            IPcfAdapter pcfAdapter,
            IVerificationTokenValidationService verificationTokenValidationService,
            IAadRequestVerificationServiceAdapter aadRvsAdapter,
            ILogger logger,
            IAppConfiguration appConfiguration)
        {
            this.pcfAdapter = pcfAdapter ?? throw new ArgumentNullException(nameof(pcfAdapter));
            this.verificationTokenValidationService = verificationTokenValidationService ?? throw new ArgumentNullException(nameof(verificationTokenValidationService));
            this.aadRvsAdapter = aadRvsAdapter ?? throw new ArgumentNullException(nameof(aadRvsAdapter));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        /// <inheritdoc />
        public async Task<IList<ServiceResponse<IQueueItem<AccountCloseRequest>>>> PostBatchAccountCloseAsync(
            IList<IQueueItem<AccountCloseRequest>> batch)
        {
            IList<ServiceResponse<IQueueItem<AccountCloseRequest>>> serviceResponseList = new List<ServiceResponse<IQueueItem<AccountCloseRequest>>>();

            if (batch.Count == 0)
            {
                // nothing to do
                return serviceResponseList;
            }

            // Wrap each queue item in a service response to keep track of status individually
            foreach (IQueueItem<AccountCloseRequest> queueItem in batch)
            {
                serviceResponseList.Add(new ServiceResponse<IQueueItem<AccountCloseRequest>> { Result = queueItem });
            }

            // Call RVS endpoint in parallel to speed up the request rate.
            var verifierResponseTasks = new List<AccountCloseBatchItem>();
            foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> request in serviceResponseList)
            {
                verifierResponseTasks.Add(new AccountCloseBatchItem(request, this.PopulateAccountCloseWithVerifierAsync(request.Result.Data)));
            }

            // Waits for all of the verifiers to be populated.
            await Task.WhenAll(verifierResponseTasks.Select(c => c.VerifierTask)).ConfigureAwait(false);

            // Handle partial failures for each queue item
            var accountCloseRequests = new List<AccountCloseRequest>();
            foreach (AccountCloseBatchItem verifierResponseTask in verifierResponseTasks)
            {
                if (!verifierResponseTask.VerifierTask.Result.IsSuccess)
                {
                    verifierResponseTask.QueueItem.Error = verifierResponseTask.VerifierTask.Result.Error;
                }
                else
                {
                    // Add the verified request to the batch.
                    accountCloseRequests.Add(verifierResponseTask.QueueItem.Result.Data);
                }
            }

            if (accountCloseRequests.Count == 0)
            {
                // it's possible they all failed.
                this.logger.Error(nameof(PcfProxyService), $"[Critical Error]: All messages failed to get verifier tokens in batch. Count: {batch.Count}");
                return serviceResponseList;
            }

            AdapterResponse pcfTask = await this.pcfAdapter.PostCommandsAsync(
                accountCloseRequests.Select(c => c).Cast<PrivacyRequest>().ToList()).ConfigureAwait(false);
            if (!pcfTask.IsSuccess)
            {
                this.logger.Error(nameof(PcfProxyService), $"Failed to send to PCF. id's: {string.Join(", ", accountCloseRequests.Select(c => c?.RequestId))}");

                // fail the entire batch since request to event grid was done via batch
                foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> response in serviceResponseList)
                {
                    // some may have already errored, so no need to override the existing error
                    if (response.Error == null)
                    {
                        response.Error = new Error
                        {
                            Code = pcfTask.Error.Code.ToString(),
                            Message = pcfTask.Error.Message
                        };
                    }
                }

                return serviceResponseList;
            }

            return serviceResponseList;
        }

        private async Task<ServiceResponse> PopulateAccountCloseWithVerifierAsync(AccountCloseRequest accountCloseRequest)
        {
            switch (accountCloseRequest.Subject)
            {
                case AadSubject aadSubject:
                    return await PopulateAndValidateVerifierAsync(accountCloseRequest, aadSubject).ConfigureAwait(false);

                case MsaSubject msaSubject:
                    throw new NotImplementedException($"Method not supported for {nameof(MsaSubject)}");

                default:
                    throw new NotImplementedException($"Method not supported for Subject: {accountCloseRequest.Subject?.GetType().FullName}");
            }
        }

        private async Task<ServiceResponse> PopulateAndValidateVerifierAsync(AccountCloseRequest accountCloseRequest, AadSubject aadSubject)
        {
            bool isMultiTenantCollaborationEnabled = await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration).ConfigureAwait(false);

            // Classify if the request is account close or account cleanup. If one of the following conditions is met, the request is considered as account close:
            // 1) Feature flag for MultiTenantCollaboration is off
            // 2) The Subject is not AadSubject2
            bool isAccountCloseRequest = !isMultiTenantCollaborationEnabled || !(aadSubject is AadSubject2);

            AadRvsRequest aadRvsRequest = PrivacyRequestConverter.CreateAadRvsGdprRequestV2(accountCloseRequest, 
                isAccountCloseRequest ? AadRvsOperationType.AccountClose : AadRvsOperationType.AccountCleanup);

            AdapterResponse<AadRvsVerifiers> aadRvsConstructAccountCloseVerifier = isAccountCloseRequest ?
                    await this.aadRvsAdapter.ConstructAccountCloseAsync(aadRvsRequest).ConfigureAwait(false) :
                    await this.aadRvsAdapter.ConstructAccountCleanupAsync(aadRvsRequest, null).ConfigureAwait(false);

            if (!aadRvsConstructAccountCloseVerifier.IsSuccess)
            {
                ErrorCode errorCode = aadRvsConstructAccountCloseVerifier.Error.Code.ToServiceErrorCode();
                return new ServiceResponse<string> { Error = new Error(errorCode, aadRvsConstructAccountCloseVerifier.Error.Message) };
            }

            if (isMultiTenantCollaborationEnabled)
            {
                var error = this.aadRvsAdapter.UpdatePrivacyRequestWithVerifiers(accountCloseRequest, aadRvsConstructAccountCloseVerifier.Result);
                if (error != null)
                {
                    return new ServiceResponse { Error = new Error(ErrorCode.PartnerError, "Missing V3 verifier") };
                }
            }
            else
            {
                // TODO: Remove once the feature flag is permanently turned on
                if (string.IsNullOrEmpty(aadRvsConstructAccountCloseVerifier.Result.V2))
                {
                    return new ServiceResponse { Error = new Error(ErrorCode.PartnerError, "Missing V2 verifier") };
                }

                accountCloseRequest.VerificationToken = aadRvsConstructAccountCloseVerifier.Result.V2;

                if (this.aadRvsAdapter.TryGetOrgIdPuid(accountCloseRequest.VerificationToken, out long orgIdPuid))

                {
                    aadSubject.OrgIdPUID = orgIdPuid;
                }
            }

            // Validate non-empty token
            if (!string.IsNullOrEmpty(accountCloseRequest.VerificationToken))
            {
                AdapterResponse isValidVerifier =
                    await this.verificationTokenValidationService.ValidateVerifierAsync(accountCloseRequest, accountCloseRequest.VerificationToken).ConfigureAwait(false);

                if (!isValidVerifier.IsSuccess)
                {
                    ErrorCode errorCode = isValidVerifier.Error.Code.ToServiceErrorCode();
                    return new ServiceResponse { Error = new Error(errorCode, isValidVerifier.Error.Message) };
                }
            }

            if (!string.IsNullOrEmpty(accountCloseRequest.VerificationTokenV3))
            {
                AdapterResponse isValidVerifier =
                    await this.verificationTokenValidationService.ValidateVerifierAsync(accountCloseRequest, accountCloseRequest.VerificationTokenV3).ConfigureAwait(false);

                if (!isValidVerifier.IsSuccess)
                {
                    ErrorCode errorCode = isValidVerifier.Error.Code.ToServiceErrorCode();
                    return new ServiceResponse { Error = new Error(errorCode, isValidVerifier.Error.Message) };
                }
            }

            return new ServiceResponse();
        }
    }
}
