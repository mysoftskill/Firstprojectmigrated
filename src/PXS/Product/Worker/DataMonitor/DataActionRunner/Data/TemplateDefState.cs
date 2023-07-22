// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Data
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    /// <summary>
    ///     state object representing a single template
    /// </summary>
    public class TemplateDefState :
        TableEntity,
        ITableEntityInitializer
    {
        private TemplateDef resultObj;

        /// <summary>
        ///     Initializes a new instance of the TemplateDefinitionState class
        /// </summary>
        public TemplateDefState()
        {
            this.PartitionKey = DataConstants.DataPartitionKey;
        }

        /// <summary>
        ///     Gets or sets the definition of a template
        /// </summary>
        public TemplateDef Template =>
            this.resultObj ??
            (this.resultObj = JsonConvert.DeserializeObject<TemplateDef>(this.TemplateDefJson));


        /// <summary>
        ///     Gets or sets tag
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        ///     Gets or sets action definition JSON
        /// </summary>
        public string TemplateDefJson { get; set; }

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

            this.TemplateDefJson = entity.GetString("TemplateDefJson");

            if (string.IsNullOrWhiteSpace(this.TemplateDefJson) == false)
            {
                this.resultObj = JsonConvert.DeserializeObject<TemplateDef>(this.TemplateDefJson);
            }
        }
    }
}
