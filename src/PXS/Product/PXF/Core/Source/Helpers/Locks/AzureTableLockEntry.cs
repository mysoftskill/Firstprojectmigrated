// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks
{
    using System;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     class to hold a table lock entry
    /// </summary>
    public class AzureTableLockEntry : TableEntity
    {
        /// <summary>
        ///     Gets or sets the time that the current lock expires
        /// </summary>
        public DateTime LockExpires { get; set; }

        /// <summary>
        ///     Gets or sets the owner task identifier
        /// </summary>
        public string OwnerTaskId { get; set; }

        /// <summary>
        ///     Gets or sets the lock group
        /// </summary>
        [IgnoreProperty]
        public string LockGroup
        {
            get => TableUtilities.UnescapeKey(this.PartitionKey);
            set => this.PartitionKey = TableUtilities.EscapeKey(value);
        }

        /// <summary>
        ///     Gets or sets the lock name
        /// </summary>
        [IgnoreProperty]
        public string LockName
        {
            get => TableUtilities.UnescapeKey(this.RowKey);
            set => this.RowKey = TableUtilities.EscapeKey(value);
        }
    }
}
