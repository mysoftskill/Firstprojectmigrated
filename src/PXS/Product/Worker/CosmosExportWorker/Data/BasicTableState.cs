// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     a basic table object used for seraching / deleting rows given basic table properties
    /// </summary>
    public class BasicTableState :
        TableEntity,
        ITableEntityInitializer
    {
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
        }
    }
}
