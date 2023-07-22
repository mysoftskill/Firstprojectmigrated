// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables
{
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     contract for extracting a table entity from an object model of one
    /// </summary>
    public interface ITableEntityStorageExtractor
    {
        /// <summary>
        ///     Geneate a representation that will be sent to storage
        /// </summary>
        /// <returns>object instance that should be sent to storage</returns>
        /// <remarks>
        ///     the representation generated can be more dynamic than the properties directly exposed by the object implementing 
        ///      this interface, which allows the object to vary the storage columns based on the object state, such as breaking 
        ///      a single column into multiple for storage purposes to work around column size limitations
        /// </remarks>
        ITableEntity ExtractStorageRepresentation();
    }
}
