// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos
{
    using System;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.CosmosHelpers;

    /// <summary>
    ///     contract for Cosmos file systems
    /// </summary>
    public interface ICosmosFileSystem : IFileSystem
    {
        /// <summary>
        ///     Gets the default lifetime for an object in the file system
        /// </summary>
        /// <remarks>null indicates no default lifetime</remarks>
        TimeSpan? DefaultLifetime { get; }

        /// <summary>
        ///     Gets the Cosmos client to access the file system
        /// </summary>
        ICosmosClient Client { get; }
    }
}
