// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.TestMsa
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    /// Interface for testing MSA operations.
    /// </summary>
    public interface ITestMsaService
    {
        /// <summary>
        ///     Post test MSA close request to PCF and DeleteFeed.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        Task<ServiceResponse<Guid>> PostTestMsaCloseAsync(
            IRequestContext requestContext
        );
    }
}
