namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// An account close command. This is semantically identical to a generic privacy command,
    /// but we specialize here to make the model congruent to Delete and Export, which do have specializations.
    /// </summary>
    public class AccountCloseCommand : PrivacyCommand
    {
        private ValidOperation operationType;

        /// <summary>
        /// Instantiates <see cref="AccountCloseCommand"/>
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
        /// <param name="queueStorageType">QueueStorageType</param>
        public AccountCloseCommand(
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
            this.operationType = ValidOperation.AccountClose;

            // Override operation type if this is an account cleanup operation
            if (subject is AadSubject2 aadSubject2)
            {
                if (aadSubject2.HomeTenantId != default && aadSubject2.HomeTenantId != aadSubject2.TenantId)
                {
                    this.operationType = ValidOperation.AccountCleanup;
                }
            }
        }

        /// <summary>
        /// The command type.
        /// </summary>
        public override PrivacyCommandType CommandType => PrivacyCommandType.AccountClose;

        /// <inheritdoc />
        protected override ValidOperation ValidationOperationType => this.operationType;

        /// <inheritdoc />
        public override bool AreDataTypesApplicable(IEnumerable<DataTypeId> dataTypeIds)
        {
            return true;
        }
    }
}
