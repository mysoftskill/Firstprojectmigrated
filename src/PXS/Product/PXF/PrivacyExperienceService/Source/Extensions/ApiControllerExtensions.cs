// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service
{
    using System;
    using System.Linq;
    using System.Security;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common;

    /// <summary>
    ///     ApiController Extensions
    /// </summary>
    internal static class ApiControllerExtensions
    {
        internal static RequestContext GetCurrentUserRequestContext(this ApiController controller)
        {
            if (controller.User == null || controller.User.Identity == null)
            {
                // This should never happen when the Authentication Attribute is used.
                throw new SecurityException("User is not authenticated.");
            }

            if (controller.User.Identity is MsaSelfIdentity msaIdentity)
            {
                return new RequestContext(
                    msaIdentity,
                    controller.Request.RequestUri,
                    controller.Request.Headers.ToDictionary(p => p.Key, p => p.Value.ToArray()));
            }

            if (controller.User.Identity is MsaSiteIdentity msaSiteIdentity)
            {
                return new RequestContext(
                    msaSiteIdentity,
                    controller.Request.RequestUri,
                    controller.Request.Headers.ToDictionary(p => p.Key, p => p.Value.ToArray()));
            }

            if (controller.User.Identity is AadIdentityWithMsaUserProxyTicket aadIdentityWithMsaUserProxyTicket)
            {
                return new RequestContext(
                    aadIdentityWithMsaUserProxyTicket,
                    controller.Request.RequestUri,
                    controller.Request.Headers.ToDictionary(p => p.Key, p => p.Value.ToArray()));
            }

            if (controller.User.Identity is AadIdentity aadIdentity)
            {
                return new RequestContext(
                    aadIdentity,
                    controller.Request.RequestUri,
                    controller.Request.Headers.ToDictionary(p => p.Key, p => p.Value.ToArray()));
            }

            throw new InvalidOperationException("Invalid identity.");
        }
    }
}
