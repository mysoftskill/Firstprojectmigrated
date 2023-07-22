// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    
    /// <summary>
    ///     contract for periodic file writer objects
    /// </summary>
    public interface IPeriodicFileWriter : IQueuedFileWriter
    {
    }
}
