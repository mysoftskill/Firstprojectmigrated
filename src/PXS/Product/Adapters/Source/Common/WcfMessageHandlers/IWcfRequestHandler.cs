// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A WCF executor is used to execute WCF operations wrapped in a passed in function.
    /// </summary>
    public interface IWcfRequestHandler
    {
        Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action);

        Task ExecuteAsync(Func<Task> action);
    }
}
