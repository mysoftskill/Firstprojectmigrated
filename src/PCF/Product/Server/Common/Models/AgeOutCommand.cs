namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// An age out command.
    /// </summary>
    public class AgeOutCommand : PrivacyCommand
    {
        /// <summary>
        /// Instantiates <see cref="AgeOutCommand"/>
        /// </summary>
        /// <param name="agentId">AgentId of the command</param>
        /// <param name="assetGroupQualifier">Asset group qualifier</param>
        /// <param name="verifier">V2 JWT token/verifier for validating the command</param>
        /// <param name="verifierV3">V3 JWT token/verifier for validating the command</param>
        /// <param name="commandId">Id of the command</param>
        /// <param name="batchId">Batch Id</param>
        /// <param name="nextVisibleTime">Next visible time for the command</param>
        /// <param name="subject">Command subject type</param>
        /// <param name="clientCommandState">Client command state</param>
        /// <param name="assetGroupId">Id of the asset group to which this command is associated with</param>
        /// <param name="correlationVector">Correlation vector to track the command</param>
        /// <param name="cloudInstance">Aad cloud to which this request is intended for</param>
        /// <param name="commandSource">Where the command was issued</param>
        /// <param name="processorApplicable">Is this command applicable to processors</param>
        /// <param name="controllerApplicable">Is this command applicable to controllers</param>
        /// <param name="absoluteExpirationTime">The absolute expiration time of the command.</param>
        /// <param name="timestamp">TimeStamp</param>
        /// <param name="lastActive">The last active time of the account.</param>
        /// <param name="queueStorageType">QueueStorageType</param>
        /// <param name="isSuspended">Whether the account was suspended at time of closure.</param>
        public AgeOutCommand(
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
            DateTimeOffset? lastActive,
            QueueStorageType queueStorageType,
            bool? isSuspended = null)
            : base(
                  agentId, 
                  assetGroupQualifier, 
                  verifier,
                  verifierV3,
                  commandId, 
                  batchId, 
                  nextVisibleTime, 
                  subject, 
                  clientCommandState, 
                  assetGroupId, 
                  correlationVector, 
                  timestamp, 
                  cloudInstance, 
                  commandSource, 
                  processorApplicable, 
                  controllerApplicable,
                  absoluteExpirationTime,
                  queueStorageType)
        {
            this.LastActive = lastActive;
            this.IsSuspended = isSuspended;
        }
        
        /// <summary>
        /// The command type.
        /// </summary>
        public override PrivacyCommandType CommandType => PrivacyCommandType.AgeOut;

        /// <summary>
        /// Gets the last time the account was active.
        /// </summary>
        public DateTimeOffset? LastActive { get; }

        /// <summary>
        /// Gets a flag that indicates that the account was suspended when aged out.
        /// </summary>
        public bool? IsSuspended { get; }

        /// <inheritdoc />
        // TODO: Fix this. This is AccountClose if the subject is MSA. Other subjects don't support it.
        protected override ValidOperation ValidationOperationType => ValidOperation.AccountClose;

        /// <inheritdoc />
        public override bool AreDataTypesApplicable(IEnumerable<DataTypeId> dataTypeIds)
        {
            return true;
        }
    }
}
