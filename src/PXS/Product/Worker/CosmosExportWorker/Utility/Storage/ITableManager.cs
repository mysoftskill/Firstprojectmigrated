// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.Storage
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     contract for table managers
    /// </summary>
    public interface ITableManager
    {
        /// <summary>
        ///     Gets a reference to table for a particular type with a particular name
        /// </summary>
        /// <typeparam name="T">table entity type</typeparam>
        /// <param name="name">table registered name</param>
        /// <returns>resulting value</returns>
        ITable<T> GetTable<T>(string name)
            where T : class, ITableEntity, new();
    }
}
