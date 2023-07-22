// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Data
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    /// <summary>
    ///     state object representing a single reference to a job to execute
    /// </summary>
    public class ActionRefState :
        TableEntity,
        ITableEntityInitializer,
        ITableEntityStorageExtractor
    {
        private ActionRefRunnable resultObj;

        /// <summary>
        ///     Initializes a new instance of the JobsToExecuteState class
        /// </summary>
        public ActionRefState()
        {
            this.PartitionKey = DataConstants.DataPartitionKey;
        }

        /// <summary>
        ///     Gets or sets a reference to an action
        /// </summary>
        public ActionRefRunnable ActionRef =>
            this.resultObj ??
            (this.resultObj = JsonConvert.DeserializeObject<ActionRefRunnable>(this.ActionRefJson));

        /// <summary>
        ///     Gets or sets tag
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     Gets or sets action definition JSON
        /// </summary>
        public string ActionRefJson { get; set; }

        /// <summary>
        ///     Initializes the class with the raw table object
        /// </summary>
        /// <param name="rawTableObject">raw table object</param>
        public void Initialize(object rawTableObject)
        {
            DynamicTableEntity entity = rawTableObject as DynamicTableEntity;

            if (entity == null)
            {
                return;
            }

            this.PartitionKey = entity.PartitionKey;
            this.Timestamp = entity.Timestamp;
            this.RowKey = entity.RowKey;
            this.ETag = entity.ETag;

            this.Id = this.RowKey;

            this.ActionRefJson = entity.GetString("ActionRefJson");

            if (string.IsNullOrWhiteSpace(this.ActionRefJson) == false)
            {
                this.resultObj = JsonConvert.DeserializeObject<ActionRefRunnable>(this.ActionRefJson);
            }
        }

        /// <summary>
        ///     Generate a representation that will be sent to storage
        /// </summary>
        /// <returns>object instance that should be sent to storage</returns>
        /// <remarks>
        ///     the representation generated can be more dynamic than the properties directly exposed by the object implementing 
        ///      this interface, which allows the object to vary the storage columns based on the object state, such as breaking 
        ///      a single column into multiple for storage purposes to work around column size limitations
        /// </remarks>
        public ITableEntity ExtractStorageRepresentation()
        {
            return null;
        }
    }
}
