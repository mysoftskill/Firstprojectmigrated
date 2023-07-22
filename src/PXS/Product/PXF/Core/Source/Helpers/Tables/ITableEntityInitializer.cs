// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables
{
    /// <summary>
    ///     contract for table entity initializers
    /// </summary>
    public interface ITableEntityInitializer
    {
        /// <summary>
        ///     Initializes the class with the raw table object
        /// </summary>
        /// <param name="rawTableObject">raw table object</param>
        void Initialize(object rawTableObject);
    }
}
