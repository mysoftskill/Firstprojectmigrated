// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for a command receiver
    /// </summary>
    public interface ICommandReceiver
    {
        /// <summary>
        ///     Begins receiving commands
        /// </summary>
        /// <param name="token">token</param>
        /// <returns>resulting value</returns>
        Task BeginReceivingAsync(CancellationToken token);
    }
}
