// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Graph
{
    /// <summary>
    ///     Graph Response.
    /// </summary>
    /// <typeparam name="T">Type of Value</typeparam>
    public abstract class GraphResponse<T> 
    {
        /// <summary>
        ///     Gets or sets Values
        /// </summary>
        public T Value { get; set; }
    }
}
