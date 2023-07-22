// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    ///     Implements <see cref="IVerificationTokenValidationService" />
    /// </summary>
    public class VerificationTokenValidationService : IVerificationTokenValidationService
    {
        private readonly string cloudInstance;

        private readonly bool enableVerificationValidationAad;

        private readonly bool enableVerificationValidationMsa;

        private readonly ILogger logger;

        private readonly TargetMsaKeyDiscoveryEnvironment targetMsaKeyDiscoveryEnvironment;

        private readonly IValidationService validationService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VerificationTokenValidationService" /> class.
        /// </summary>
        public VerificationTokenValidationService(IPrivacyConfigurationManager privacyConfiguration, ILogger logger, IValidationServiceFactory validationServiceFactory)
        {
            if (privacyConfiguration == null) throw new ArgumentNullException(nameof(privacyConfiguration));

            IVerificationValidationServiceConfig config = privacyConfiguration.AdaptersConfiguration.VerificationValidationServiceConfiguration;

            this.targetMsaKeyDiscoveryEnvironment = config.TargetMsaKeyDiscoveryEnvironment;
            this.enableVerificationValidationAad = config.EnableVerificationCheckAad;
            this.enableVerificationValidationMsa = config.EnableVerificationCheckMsa;

            var pcvEnvironment = (this.targetMsaKeyDiscoveryEnvironment == TargetMsaKeyDiscoveryEnvironment.MsaProd) ? PcvEnvironment.Production : PcvEnvironment.Preproduction;
            this.validationService = validationServiceFactory.Create(pcvEnvironment);

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.cloudInstance = CloudInstanceMapper.ToPcfCloudInstance(privacyConfiguration.PrivacyExperienceServiceConfiguration.CloudInstance);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse> ValidateVerifierAsync(PrivacyRequest privacyRequest, string verificationToken)
        {
            if (privacyRequest == null)
                throw new ArgumentNullException(nameof(privacyRequest));

            const string DisabledWarningMessage = "Verification of verifier token is disabled.";
            switch (privacyRequest.Subject)
            {
                case AadSubject aadSubject:
                    if (!this.enableVerificationValidationAad)
                    {
                        this.logger.Warning(nameof(VerificationTokenValidationService), DisabledWarningMessage);
                        return new AdapterResponse();
                    }

                    break;

                case MsaSubject msaSubject:
                case DeviceSubject deviceSubject:
                    if (!this.enableVerificationValidationMsa)
                    {
                        this.logger.Warning(nameof(VerificationTokenValidationService), DisabledWarningMessage);
                        return new AdapterResponse();
                    }

                    switch (this.targetMsaKeyDiscoveryEnvironment)
                    {
                        case TargetMsaKeyDiscoveryEnvironment.MsaInt:
                        case TargetMsaKeyDiscoveryEnvironment.MsaProd:

                            // In environments where MSA gives our site access to retrieve verifier tokens, attempt to validate them.
                            // But if the request came from a watchdog, and it has no verifier token, then no validation is done.
                            if (privacyRequest.IsWatchdogRequest && string.IsNullOrWhiteSpace(verificationToken))
                            {
                                return new AdapterResponse();
                            }

                            break;
                        case TargetMsaKeyDiscoveryEnvironment.None:

                            // None means it's not supported. We would use this in our PPE where we do not access from MSA to retrieve verifier tokens for any MSA requests.
                            return new AdapterResponse();
                        default:
                            var errorMessage = $"Invalid {nameof(this.targetMsaKeyDiscoveryEnvironment)} key discovery specified in configuration.";
                            new ErrorEvent
                            {
                                ComponentName = nameof(VerificationTokenValidationService),
                                ErrorName = AdapterErrorCode.Unknown.ToString(),
                                ErrorMethod = nameof(this.ValidateVerifierAsync),
                                ErrorMessage = errorMessage,
                                ErrorType = AdapterErrorCode.Unknown.ToString()
                            }.LogError();
                            return new AdapterResponse
                            {
                                Error = new AdapterError(
                                    AdapterErrorCode.Unknown,
                                    errorMessage,
                                    500)
                            };
                    }

                    break;

                case DemographicSubject demographicSubject:
                case MicrosoftEmployee microsoftEmployeeSubject:

                    // nothing to do for this type
                    break;
            }

            ValidOperation operation;
            switch (privacyRequest.RequestType)
            {
                case RequestType.Delete:
                    operation = ValidOperation.Delete;
                    break;

                case RequestType.AccountClose:
                    operation = (privacyRequest.Subject is AadSubject2 aadSubject2 && aadSubject2.TenantIdType == TenantIdType.Resource) ? ValidOperation.AccountCleanup : ValidOperation.AccountClose;
                    break;

                case RequestType.Export:
                    operation = ValidOperation.Export;
                    break;

                case RequestType.AgeOut:
                    operation = privacyRequest.Subject is MsaSubject ? ValidOperation.AccountClose : ValidOperation.AgeOut;
                    break;

                default:

                    var errorMessage = $"Request type {privacyRequest.RequestType} is not supported.";
                    new ErrorEvent
                    {
                        ComponentName = nameof(VerificationTokenValidationService),
                        ErrorName = AdapterErrorCode.InvalidInput.ToString(),
                        ErrorMethod = nameof(this.ValidateVerifierAsync),
                        ErrorMessage = errorMessage,
                        ErrorType = AdapterErrorCode.InvalidInput.ToString()
                    }.LogError();
                    return new AdapterResponse(
                        new AdapterError(AdapterErrorCode.InvalidInput, errorMessage, 0));
            }

            if (string.IsNullOrWhiteSpace(verificationToken))
            {
                var errorMessage = $"{nameof(verificationToken)} cannot be empty.";
                new ErrorEvent
                {
                    ComponentName = nameof(VerificationTokenValidationService),
                    ErrorName = AdapterErrorCode.NullVerifier.ToString(),
                    ErrorMethod = nameof(this.ValidateVerifierAsync),
                    ErrorMessage = errorMessage,
                    ErrorType = AdapterErrorCode.NullVerifier.ToString()
                }.LogError();
                return new AdapterResponse(
                    new AdapterError(AdapterErrorCode.NullVerifier, errorMessage, 0));
            }

            try
            {
                DataTypeId dataType = GetDataTypeIdFromPrivacyRequest(privacyRequest);

                await this.validationService.EnsureValidAsync(
                    verificationToken,
                    new CommandClaims
                    {
                        CommandId = privacyRequest.RequestId.ToString(),
                        Subject = privacyRequest.Subject,
                        Operation = operation,
                        AzureBlobContainerTargetUri = (privacyRequest as ExportRequest)?.StorageUri,
                        CloudInstance = this.cloudInstance,
                        ControllerApplicable = privacyRequest.ControllerApplicable,
                        ProcessorApplicable = privacyRequest.ProcessorApplicable,
                        DataType = dataType
                    },
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (InvalidPrivacyCommandException invalidPrivacyCommandException)
            {
                var errorMessage = "The verification token is invalid.";
                this.logger.Error(nameof(VerificationTokenValidationService), invalidPrivacyCommandException, errorMessage);
                new ErrorEvent
                {
                    ComponentName = nameof(VerificationTokenValidationService),
                    ErrorName = nameof(InvalidPrivacyCommandException),
                    ErrorMethod = nameof(this.ValidateVerifierAsync),
                    ErrorMessage = invalidPrivacyCommandException.Message,
                    ErrorType = invalidPrivacyCommandException.GetType().FullName,
                    ErrorCode = invalidPrivacyCommandException.HResult.ToString(),
                    CallStack = invalidPrivacyCommandException.StackTrace
                }.LogError();
                return new AdapterResponse(new AdapterError(AdapterErrorCode.Unknown, invalidPrivacyCommandException.ToString(), 0));
            }
            catch (KeyDiscoveryException keyDiscoveryException)
            {
                // the validation could not be completed for internal reasons. Let us return true as it will be revalidated downstream and will fail the process at that point if it still cant be validated.
                this.logger.Error(nameof(VerificationTokenValidationService), keyDiscoveryException, "Key discovery encountered an error.");
                new ErrorEvent
                {
                    ComponentName = nameof(VerificationTokenValidationService),
                    ErrorName = nameof(KeyDiscoveryException),
                    ErrorMethod = nameof(this.ValidateVerifierAsync),
                    ErrorMessage = keyDiscoveryException.Message,
                    ErrorType = keyDiscoveryException.GetType().FullName,
                    ErrorCode = keyDiscoveryException.HResult.ToString(),
                    CallStack = keyDiscoveryException.StackTrace
                }.LogError();
            }
            catch (ArgumentException argumentException)
            {
                var errorMessage = $"Argument exception may indicate verifier was null. Value was: {verificationToken}";
                this.logger.Error(nameof(VerificationTokenValidationService), argumentException, errorMessage);
                new ErrorEvent
                {
                    ComponentName = nameof(VerificationTokenValidationService),
                    ErrorName = nameof(ArgumentException),
                    ErrorMethod = nameof(this.ValidateVerifierAsync),
                    ErrorMessage = argumentException.Message,
                    ErrorType = argumentException.GetType().FullName,
                    ErrorCode = argumentException.HResult.ToString(),
                    CallStack = argumentException.StackTrace
                }.LogError();
                return new AdapterResponse(new AdapterError(AdapterErrorCode.NullVerifier, errorMessage, 0));
            }
            catch (Exception e)
            {
                new ErrorEvent
                {
                    ComponentName = nameof(VerificationTokenValidationService),
                    ErrorName = nameof(Exception),
                    ErrorMethod = nameof(this.ValidateVerifierAsync),
                    ErrorMessage = e.Message,
                    ErrorType = e.GetType().FullName,
                    ErrorCode = e.HResult.ToString(),
                    CallStack = e.StackTrace
                }.LogError();
                return new AdapterResponse(new AdapterError(AdapterErrorCode.Unknown, e.ToString(), 500));
            }

            return new AdapterResponse();
        }

        internal static DataTypeId GetDataTypeIdFromPrivacyRequest(PrivacyRequest privacyRequest)
        {
            string privacyDataTypeStr = (privacyRequest as DeleteRequest)?.PrivacyDataType;

            if (privacyDataTypeStr == null)
            {
                return null;
            }

            DataTypeId dataType = null;
            Policies.Current?.DataTypes.TryCreateId(privacyDataTypeStr, out dataType);
            return dataType;
        }
    }
}
