namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// An export command.
    /// </summary>
    public class ExportCommand : PrivacyCommand
    {
        private Uri azureBlobUri;
        private string azureBlobPath;

        public ExportCommand(
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
            IEnumerable<DataTypeId> dataTypes,
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
            this.DataTypeIds = new HashSet<DataTypeId>(dataTypes);
        }

        /// <summary>
        /// The Azure Blob URI to export to.
        /// </summary>
        public Uri AzureBlobContainerTargetUri
        {
            get
            {
                return this.azureBlobUri;
            }

            set
            {
                this.azureBlobUri = value;
            }
        }

        /// <summary>
        /// The Azure Blob Path to export to.
        /// </summary>
        public string AzureBlobContainerPath
        {
            get
            {
                return this.azureBlobPath;
            }

            set
            {
                this.azureBlobPath = value;
            }
        }
        
        /// <summary>
        /// The command type.
        /// </summary>
        public override PrivacyCommandType CommandType => PrivacyCommandType.Export;

        /// <inheritdoc />
        protected override ValidOperation ValidationOperationType => ValidOperation.Export;

        /// <inheritdoc />
        public override bool AreDataTypesApplicable(IEnumerable<DataTypeId> dataTypeIds)
        {
            if (!this.DataTypeIds.Any())
            {
                // Do not match commands that have been reduced to no data types
                return false;
            }

            return dataTypeIds.Contains(Policies.Current.DataTypes.Ids.Any)
                   || dataTypeIds.Any(dt => this.DataTypeIds.Contains(dt));
        }
    }
}
