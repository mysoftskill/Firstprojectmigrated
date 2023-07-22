// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     represents stat
    /// </summary>
    /// <remarks>
    ///     Partition key contains the AgentId
    ///     Row key contains the CommandId
    /// </remarks>
    public class CommandState :
        TableEntity,
        ITableEntityInitializer
    {
        /// <summary>
        ///     Gets or sets the PCF lease Id
        /// </summary>
        public string LeaseReceipt { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the PCF command is complete
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to ignore all data associated with the command
        /// </summary>
        public bool IgnoreCommand { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the command is applicable or not
        /// </summary>
        public bool NotApplicable { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether there was a non-transient error in processing the command when copying
        ///      from Cosmos to blob store
        /// </summary>
        public bool NonTransientError { get; set; }

        /// <summary>
        ///     Gets or sets the agent id
        /// </summary>
        [IgnoreProperty]
        public string AgentId
        {
            get => TableUtilities.UnescapeKey(this.PartitionKey);
            set => this.PartitionKey = TableUtilities.EscapeKey(value);
        }

        /// <summary>
        ///     Gets or sets the command id
        /// </summary>
        [IgnoreProperty]
        public string CommandId
        {
            get => TableUtilities.UnescapeKey(this.RowKey);
            set => this.RowKey = TableUtilities.EscapeKey(value);
        }

        /// <summary>
        ///     Initializes the class with the raw table object
        /// </summary>
        /// <param name="rawTableObject">raw table object</param>
        public void Initialize(object rawTableObject)
        {
            DynamicTableEntity entity = rawTableObject as DynamicTableEntity;

            if (entity == null)
            {
                throw new ArgumentException(
                    "rawTableObject was expected to be of type {0} but was instead found to be of type {1}"
                        .FormatInvariant(
                            typeof(DynamicTableEntity).Name,
                            rawTableObject.GetType().FullName));
            }

            this.PartitionKey = entity.PartitionKey;
            this.Timestamp = entity.Timestamp;
            this.RowKey = entity.RowKey;
            this.ETag = entity.ETag;

            this.NonTransientError = entity.GetBool("NonTransientError") ?? false;
            this.IgnoreCommand = entity.GetBool("IgnoreCommand") ?? false;
            this.NotApplicable = entity.GetBool("NotApplicable") ?? false;
            this.LeaseReceipt = entity.GetString("LeaseReceipt") ?? string.Empty;
            this.IsComplete = entity.GetBool("IsComplete") ?? false;
        }
    }
}
