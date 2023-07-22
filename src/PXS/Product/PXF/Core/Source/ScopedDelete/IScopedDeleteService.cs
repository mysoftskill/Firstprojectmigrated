// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.ScopedDelete
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    ///     Defines an interface for a ScopedDelete Service.
    /// </summary>
    public interface IScopedDeleteService
    {
        /// <summary>
        ///     Handles a Search Requests and Query scoped delete DSR request asynchronously.
        /// </summary>
        /// <param name="requestContext">The privacy request context.</param>
        /// <param name="searchRequestsAndQueryIds">The Search Requests and Query ids. If this list is null or empty, a bulk delete is executed.</param>
        /// <returns>The Service Reponse task.</returns>
        Task<ServiceResponse> SearchRequestsAndQueryScopedDeleteAsync(IRequestContext requestContext, IEnumerable<string> searchRequestsAndQueryIds);
    }
}
