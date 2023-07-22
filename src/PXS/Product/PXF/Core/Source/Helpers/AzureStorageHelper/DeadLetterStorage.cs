// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    public class DeadLetterStorage<T> : TableEntity, ITableEntityInitializer
    {
        /// <summary>
        ///     Gets or sets the Data object in JSON Format
        /// </summary>
        public string Data
        {
            get => this.DataActual != null ? JsonConvert.SerializeObject(this.DataActual) : string.Empty;

            set => this.DataActual = value != null ? JsonConvert.DeserializeObject<T>(value) : default(T);
        }

        /// <summary>
        ///     Gets or sets the Data object
        /// </summary>
        [IgnoreProperty]
        public T DataActual { get; set; }

        /// <summary>
        ///     Gets or sets the error code.
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        ///     Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes the class with the raw table object
        /// </summary>
        /// <param name="rawTableObject">raw table object</param>
        public void Initialize(object rawTableObject)
        {
            if (rawTableObject is DynamicTableEntity entity)
            {
                this.PartitionKey = entity.PartitionKey;
                this.Timestamp = entity.Timestamp;
                this.RowKey = entity.RowKey;
                this.ETag = entity.ETag;

                this.Data = entity.GetString("Data") ?? string.Empty;
                this.ErrorCode = entity.GetString("ErrorCode") ?? string.Empty;
                this.ErrorMessage = entity.GetString("ErrorMessage") ?? string.Empty;
            }
        }
    }
}
