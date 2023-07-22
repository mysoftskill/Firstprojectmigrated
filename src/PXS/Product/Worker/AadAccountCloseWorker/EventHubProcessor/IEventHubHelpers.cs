// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventHubHelpers
    {
        /// <summary>
        ///     Gets a collection of <see cref="IConnectionInformation"/> for all the Event Hubs
        /// </summary>
        /// <returns>A collection of <see cref="IConnectionInformation"/> for all the Event Hubs</returns>
        Task<IEnumerable<IConnectionInformation>> GetConnectionInformationsAsync();

        /// <summary>
        ///     Gets the connection string for azure storage
        /// </summary>
        /// <returns>The connection string for azure storage</returns>
        Task<string> GetAzureStorageConnectionStringAsync();
    }
}
