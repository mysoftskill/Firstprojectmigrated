// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    
    /// <summary>
    ///    contact for file system directories
    /// </summary>
    public interface IDirectory : IFileSystemObject
    {
        /// <summary>
        ///     Enumerates the sub-objects of the directory
        /// </summary>
        /// <returns>resulting value</returns>
        Task<ICollection<IFileSystemObject>> EnumerateAsync();
    }
}
