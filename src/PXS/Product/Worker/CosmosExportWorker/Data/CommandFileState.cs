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
    ///     indicates the number of rows processed for a given command in a single data file
    /// </summary>
    public class CommandFileState :
        TableEntity,
        ITableEntityInitializer
    {
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
        ///     Gets or sets the approxmiate byte count written
        /// </summary>
        public long ByteCount { get; set; }

        /// <summary>
        ///     Gets or sets the approxmiate byte count written
        /// </summary>
        public string CommandId { get; set; }

        /// <summary>
        ///     Gets or sets the data file path
        /// </summary>
        [IgnoreProperty]
        public string DataFilePathAndCommand
        {
            get => TableUtilities.UnescapeKey(this.RowKey);
            set => this.RowKey = TableUtilities.EscapeKey(value);
        }

        /// <summary>
        ///     Gets or sets the file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        ///     Gets or sets the approxmiate byte count written
        /// </summary>
        public string NonTransientErrorInfo { get; set; }

        /// <summary>
        ///     Gets or sets row count
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        ///     Initializes the class with the raw table object
        /// </summary>
        /// <param name="rawTableObject">raw table object</param>
        public void Initialize(object rawTableObject)
        {
            DynamicTableEntity entity = rawTableObject as DynamicTableEntity;
            EntityProperty prop;

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

            this.NonTransientErrorInfo = entity.GetString("NonTransientErrorInfo");
            this.CommandId = entity.GetString("CommandId") ?? string.Empty;

            // default this to 1 because we had to have written something for the data file if we have a row at all. This is true
            //  for ByteCount as well below.
            this.RowCount = entity.GetInt("RowCount") ?? 1;

            this.FilePath = entity.GetString("FilePath") ?? string.Empty;

            // this has been ugpraded from a int to a long to support far greater than expected write sizes, but we need
            //  to expect to get back some ints from older rows
            if (entity.Properties.TryGetValue("ByteCount", out prop))
            {
                switch (prop.PropertyType)
                {
                    case EdmType.Int64:
                        this.ByteCount = prop.Int64Value ?? 1;
                        break;
                    case EdmType.Int32:
                        this.ByteCount = prop.Int32Value ?? 1;
                        break;
                    default:
                        this.ByteCount = 1;
                        break;
                }
            }
            else
            {
                this.ByteCount = 1;
            }
        }
    }
}
