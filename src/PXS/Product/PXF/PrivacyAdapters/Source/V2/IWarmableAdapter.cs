// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.V2
{
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;

    public interface IWarmableAdapter
    {
        /// <summary>
        ///     Warmup the remote system.
        /// </summary>
        Task WarmupAsync(IPxfRequestContext requestContext, ResourceType resourceType);
    }
}
