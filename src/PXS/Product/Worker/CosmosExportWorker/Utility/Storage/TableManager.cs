// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.Storage
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     TableManager class
    /// </summary>
    public class TableManager : ITableManager
    {
        private readonly IDependencyManager container;

        /// <summary>
        ///     Initializes a new instance of the TableManager class
        /// </summary>
        /// <param name="container">unity container</param>
        public TableManager(IDependencyManager container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        ///     Gets a reference to table for a particular type with a particular name
        /// </summary>
        /// <typeparam name="T">table entity type</typeparam>
        /// <param name="name">table registered name</param>
        /// <returns>resulting value</returns>
        public ITable<T> GetTable<T>(string name)
            where T : class, ITableEntity, new()
        {
            return this.container.GetType<ITable<T>>(name);
        }
    }
}
