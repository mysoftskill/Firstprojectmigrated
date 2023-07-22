// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISamplingManager
    {
        /// <summary>
        ///     Applies the sampling to the action
        /// </summary>
        /// <param name="identifier">Identifier for the component that might be sampled</param>
        /// <param name="action">The action.</param>
        /// <returns>A task</returns>
        Task ApplySamplingAsync(string identifier, Func<Task> action);

        /// <summary>
        ///     Applies sampling to the collection
        /// </summary>
        /// <typeparam name="TValue">The type to sample</typeparam>
        /// <param name="identifier">Identifier for the component that might be sampled</param>
        /// <param name="values">The values to sample</param>
        /// <returns>A sampled collection</returns>
        IEnumerable<TValue> ApplySamplingToCollection<TValue>(string identifier, IEnumerable<TValue> values);
    }
}
