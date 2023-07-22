//---------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// This class serves as the base for all entities in the system. Every entity must implement this.
    /// The property "EntityId" is the unique identifier for the entity and it is used to compute the RowKey and PartitionKey
    /// for the Azure Table Storage, when they are not specified.
    /// </summary>
    public abstract class TableEntityBase
    {
        protected TableEntityBase()
        {
            this.Entity = new DynamicTableEntity();
        }

        public DynamicTableEntity Entity { get; internal set; }

        /// <summary>
        /// Equals implementation
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>true if objects are equal. False otherwise</returns>
        public override bool Equals(object obj)
        {
            var modelEntity = obj as TableEntityBase;

            if (modelEntity == null)
            {
                return false;
            }

            if (this.Entity.RowKey == modelEntity.Entity.RowKey)
            {
                return true;
            }

            if (this.Entity.RowKey == null)
            {
                return false;
            }

            return this.Entity.RowKey.Equals(modelEntity.Entity.RowKey);
        }

        /// <summary>
        /// Hash code implementation for TableEntityBase.
        /// </summary>
        /// <returns>The hash code for the entity</returns>
        public override int GetHashCode()
        {
            return (this.Entity.RowKey ?? string.Empty).GetHashCode();
        }
    }
}
