// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Data
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    /// <summary>
    ///     state object representing a single action
    /// </summary>
    public class ActionDefState :
        TableEntity,
        ITableEntityInitializer
    {
        private ActionDef resultObj;

        /// <summary>
        ///     Initializes a new instance of the ActionDefinitionState class
        /// </summary>
        public ActionDefState()
        {
            this.PartitionKey = DataConstants.DataPartitionKey;
        }

        /// <summary>
        ///     Gets or sets the definition of an action
        /// </summary>
        public ActionDef Action => 
            this.resultObj ?? 
            (this.resultObj = JsonConvert.DeserializeObject<ActionDef>(this.ActionDefJson));

        /// <summary>
        ///     Gets or sets tag
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        ///     Gets or sets action definition JSON
        /// </summary>
        public string ActionDefJson { get; set; }

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

            this.Tag = this.RowKey;

            this.ActionDefJson = entity.GetString("ActionDefJson");

            if (string.IsNullOrWhiteSpace(this.ActionDefJson) == false)
            {
                this.resultObj = JsonConvert.DeserializeObject<ActionDef>(this.ActionDefJson);
            }
        }
    }
}
