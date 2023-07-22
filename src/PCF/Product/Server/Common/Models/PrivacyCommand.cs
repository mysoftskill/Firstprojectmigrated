// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// Represents a generic privacy command.
    /// </summary>
    public abstract class PrivacyCommand
    {
        private AgentId agentId;
        private AssetGroupId assetGroupId;
        private CommandId commandId;
        private RequestBatchId requestBatchId;
        private IPrivacySubject subject;
        private string correlationVector;
        private DateTimeOffset timestamp;
        private DateTimeOffset nextVisibleTime;
        private bool isSyntheticTestCommand;
        private IEnumerable<DataTypeId> dataTypes;

        protected PrivacyCommand(
            AgentId agentId,
            string assetGroupQualifier,
            string verifier,
            string verifierV3,
            CommandId commandId,
            RequestBatchId batchId,
            DateTimeOffset nextVisibleTime,
            IPrivacySubject subject,
            string clientCommandState,
            AssetGroupId assetGroupId,
            string correlationVector,
            DateTimeOffset timestamp,
            string cloudInstance,
            string commandSource,
            bool? processorApplicable,
            bool? controllerApplicable,
            DateTimeOffset absoluteExpirationTime,
            QueueStorageType queueStorageType)
        {
            this.AgentId = agentId;
            this.AssetGroupQualifier = assetGroupQualifier;
            this.Verifier = verifier;
            this.VerifierV3 = verifierV3;
            this.CommandId = commandId;
            this.RequestBatchId = batchId;
            this.NextVisibleTime = nextVisibleTime;
            this.Subject = subject;
            this.AgentState = clientCommandState ?? string.Empty;
            this.AssetGroupId = assetGroupId;
            this.CorrelationVector = correlationVector;
            this.CloudInstance = cloudInstance;
            this.CommandSource = commandSource;
            this.Timestamp = timestamp;
            this.ProcessorApplicable = processorApplicable;
            this.ControllerApplicable = controllerApplicable;
            this.AbsoluteExpirationTime = absoluteExpirationTime;
            this.QueueStorageType = queueStorageType;
        }

        /// <summary>
        /// The lease receipt. Set when the command is dequeued.
        /// </summary>
        public LeaseReceipt LeaseReceipt { get; set; }

        /// <summary>
        /// The agent ID of who the command is for.
        /// </summary>
        public AgentId AgentId
        {
            get
            {
                return this.agentId;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("AgentID may not be null.", nameof(value));
                }

                this.agentId = value;
            }
        }

        /// <summary>
        /// The unique ID of the command.
        /// </summary>
        public CommandId CommandId
        {
            get
            {
                return this.commandId;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("Command ID may not be null or empty.", nameof(value));
                }

                this.commandId = value;
            }
        }

        /// <summary>
        /// The ID of the batch that this command is part of.
        /// </summary>
        public RequestBatchId RequestBatchId
        {
            get
            {
                return this.requestBatchId;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("RequestBatchId may not be null or empty.", nameof(value));
                }

                this.requestBatchId = value;
            }
        }

        /// <summary>
        /// Indicates whether this command applies to processor data.
        /// </summary>
        public bool? ProcessorApplicable { get; set; }

        /// <summary>
        /// Indicates whether this command applies to controller data.
        /// </summary>
        public bool? ControllerApplicable { get; set; }

        /// <summary>
        /// The time at which this command was created.
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get => this.timestamp;
            set => this.timestamp = value.ToUniversalTime();
        }

        /// <summary>
        /// The next time at which this command will be visible on the queue.
        /// </summary>
        public DateTimeOffset NextVisibleTime
        {
            get => this.nextVisibleTime;
            set => this.nextVisibleTime = value.ToUniversalTime();
        }

        /// <summary>
        /// The absolute expiration time of the command
        /// </summary>
        public DateTimeOffset AbsoluteExpirationTime { get; private set; }

        /// <summary>
        /// The CV used when creating this command.
        /// </summary>
        public string CorrelationVector
        {
            get
            {
                return this.correlationVector;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "NullOrEmptyPxsCv").Increment();
                    value = "(null)";
                }

                this.correlationVector = value;
            }
        }

        /// <summary>
        /// The subject of this command.
        /// </summary>
        public IPrivacySubject Subject
        {
            get
            {
                return this.subject;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("Subject may not be null.", nameof(value));
                }

                this.subject = value;
            }
        }

        /// <summary>
        /// Custom agent state we are storing on behalf of the agent.
        /// </summary>
        public string AgentState { get; set; }

        /// <summary>
        /// The ID of the asset group that this command is targeting.
        /// </summary>
        public AssetGroupId AssetGroupId
        {
            get
            {
                return this.assetGroupId;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("Asset Group ID may not be null.", nameof(value));
                }

                this.assetGroupId = value;
            }
        }

        /// <summary>
        /// Asset group qualifier.
        /// </summary>
        public string AssetGroupQualifier { get; set; }

        /// <summary>
        /// V2 Verifier string.
        /// </summary>
        public string Verifier { get; set; }

        /// <summary>
        /// V3 Verifier string.
        /// </summary>
        public string VerifierV3 { get; set; }

        /// <summary>
        /// Variants that the agent can apply to this command
        /// </summary>
        public IList<IAssetGroupVariantInfo> ApplicableVariants { get; set; }

        /// <summary>
        /// The type of this command.
        /// </summary>
        public abstract PrivacyCommandType CommandType { get; }

        /// <summary>
        /// The queue storage type for this command
        /// </summary>
        public QueueStorageType QueueStorageType { get; private set; }

        /// <summary>
        /// Choose the QueueStorageType
        /// </summary>
        public void ChooseQueueStorageType()
        {
            this.QueueStorageType = new QueueStorageTypeSelector().Process(this);
        }

        /// <summary>
        /// A non-persistent flag that indicates whether this command is synthetic or not. This flag is used during actionability filtering in PPE.
        /// </summary>
        public bool IsSyntheticTestCommand
        {
            get
            {
                return this.isSyntheticTestCommand;
            }

            set
            {
                this.isSyntheticTestCommand = value;
            }
        }

        /// <summary>
        /// Privacy Policy Contracts DataTypes
        /// </summary>
        public IEnumerable<DataTypeId> DataTypeIds
        {
            get
            {
                if (this.dataTypes == null)
                {
                    this.dataTypes = new HashSet<DataTypeId>();
                }

                return this.dataTypes;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("Command DataTypeIds may not be null.", nameof(value));
                }

                if (value.Any(x => x == null))
                {
                    throw new ArgumentException("Command DataTypeIds may not include a null data type", nameof(value));
                }

                this.dataTypes = value;
            }
        }

        /// <summary>
        /// Instance of the AAD cloud this request is intended for
        /// </summary>
        public string CloudInstance { get; set; }

        /// <summary>
        /// Source/Portal from which the command has been issued
        /// </summary>
        public string CommandSource { get; set; }

        /// <summary>
        /// Gets the validation operation type to use when checking verifiers.
        /// </summary>
        protected abstract ValidOperation ValidationOperationType
        {
            get;
        }

        /// <summary>
        /// Are these datatypes applicable to this command
        /// </summary>
        public abstract bool AreDataTypesApplicable(IEnumerable<DataTypeId> dataTypeIds);

        /// <summary>
        /// Uses the given validator to check the verifier of this command.
        /// </summary>
        public async Task<bool> IsVerifierValidAsync(IValidationService validator)
        {
            try
            {
                var verifier = (this.Subject is AadSubject2) ? this.VerifierV3 : this.Verifier;

                await validator.EnsureValidAsync(
                    verifier,
                    new CommandClaims
                    {
                        CommandId = this.CommandId.Value,
                        Operation = this.ValidationOperationType,
                        Subject = this.Subject,
                        AzureBlobContainerTargetUri = (this as ExportCommand)?.AzureBlobContainerTargetUri,

                        // If the value is null, then default to 'false' for validation purposes.
                        ControllerApplicable = this.ControllerApplicable ?? false,
                        ProcessorApplicable = this.ProcessorApplicable ?? false,
                        CloudInstance = this.CloudInstance,
                        DataType = (this as DeleteCommand)?.DataTypeIds.FirstOrDefault()
                    },
                    CancellationToken.None);

                return true;
            }
            catch (Exception ex) when (ex is KeyDiscoveryException || ex is InvalidPrivacyCommandException || ex is OperationCanceledException || ex is ArgumentException)
            {
                var justification = $"{ex.GetType().Name}, {ex.Message}";
                Logger.Instance?.InvalidVerifierReceived(this.CommandId, this.AgentId, ex);
                IncomingEvent.Current?.SetProperty("InvalidVerifierReason", justification);
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "InvalidVerifier").Increment(ex.GetType().Name);

                return false;
            }
        }

        /// <summary>
        /// Gets the list of variants applicable to this command from the list of variants
        /// </summary>
        /// <param name="assetGroupVariantInfos">List of variantInfos that need to checked for applicability</param>
        /// <param name="isPcfAppliedVariant">is it a Pcf applied variant or an agent applied variant?</param>
        /// <returns>List of applicable variantInfo</returns>
        public List<IAssetGroupVariantInfo> GetApplicableVariantsFromList(IList<IAssetGroupVariantInfo> assetGroupVariantInfos, bool isPcfAppliedVariant)
        {
            return assetGroupVariantInfos.Where(variant => variant.IsApplicableToCommand(this, isPcfAppliedVariant)).ToList();
        }

        /// <summary>
        /// Validate the variants claimed by the agent with the approved variants and if there is at least one match, return true
        /// </summary>
        /// <param name="claimedVariants">List of variants claimed by the agent</param>
        /// <param name="assetGroupInfo">AssetGroup info associated with this command</param>
        /// <returns>true if at least on variant claimed is valid else false</returns>
        public bool AreClaimedVariantsValid(string[] claimedVariants, IAssetGroupInfo assetGroupInfo)
        {
            if (claimedVariants == null || claimedVariants.Length == 0)
            {
                return true;
            }

            bool foundValidClaim = false;
            List<string> claimedVariantsToValidate = claimedVariants.ToList();

            // If the agent is claiming any variants that should be applied by PCF accept it but log
            var pcfAppliedVariants = new List<string>();

            foreach (var variant in claimedVariants)
            {
                IAssetGroupVariantInfo applicableVariant = assetGroupInfo.VariantInfosAppliedByAgents.FirstOrDefault(v => v.VariantId.GuidValue.Equals(Guid.Parse(variant)));

                if (applicableVariant == null)
                {
                    // see if it is a variant pcf applies
                    applicableVariant = assetGroupInfo.VariantInfosAppliedByPcf.FirstOrDefault(v => v.VariantId.GuidValue.Equals(Guid.Parse(variant)));

                    if (applicableVariant != null)
                    {
                        pcfAppliedVariants.Add(variant);
                    }
                }

                if (applicableVariant != null && applicableVariant.IsApplicableToCommand(this, false))
                {
                    foundValidClaim = true;
                    claimedVariantsToValidate.Remove(variant);
                }
            }

            if (pcfAppliedVariants.Count > 0)
            {
                // lets log the pcf applied variants claimed by the agent
                var variants = string.Join(",", pcfAppliedVariants);
                DualLogger.Instance.Error(nameof(PrivacyCommand), $"Agent '{this.agentId}' claimed variantsAppliedByPCF '{variants}' for assetgroup: '{this.assetGroupId}'");
                IncomingEvent.Current?.SetProperty("InvalidVariantsClaimed", $"Variants are approved for PCF to apply: {variants}");
            }

            if (claimedVariantsToValidate.Count > 0)
            {
                // lets log the invalid variants claimed by the agent
                var variants = string.Join(", ", claimedVariantsToValidate);
                DualLogger.Instance.Error(nameof(PrivacyCommand), $"Agent '{this.agentId}' claimed additional unapproved variants'{variants}' for assetgroup: '{this.assetGroupId}'");
                IncomingEvent.Current?.SetProperty("InvalidVariantsClaimed", $"{variants}");
            }

            return foundValidClaim;
        }


        /// <summary>
        /// Get PCF Command applicable variants using SAL.
        /// </summary>
        /// <param name="pcfVariants">PCF variants.</param>
        /// <returns>Collection of PCF Command applicable variants.</returns>
        public IEnumerable<IAssetGroupVariantInfo> GetCommandApplicableVariants(IEnumerable<IAssetGroupVariantInfo> pcfVariants)
        {
            SignalInfo signal = this.ToSignalInfo();

            return pcfVariants.Where(
                v => v.ToSignalApplicabilityVariantInfo().IsVariantApplicableToSignal(signal) || v.ToSignalApplicabilityVariantInfo().IsVariantPartiallyApplicableToSignal(signal));
        }
    }
}
