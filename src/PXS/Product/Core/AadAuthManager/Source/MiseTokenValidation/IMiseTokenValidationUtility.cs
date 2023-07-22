// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    public interface IMiseTokenValidationUtility
    {
        Task<ClaimsPrincipal> AuthenticateAsync(string authorizationHeaderContent, CancellationToken cancellationToken = default);
    }
}