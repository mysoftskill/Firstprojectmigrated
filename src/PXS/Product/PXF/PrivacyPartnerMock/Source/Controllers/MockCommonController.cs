// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System.Security;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.PrivacyMockService.Security;

    public class MockCommonController : ApiController
    {
        /// <summary>
        ///     Validates that the the user identity has been authenticated and returns the identity object
        /// </summary>
        /// <param name="userId">User Id paramter</param>
        /// <returns>Msa self-auth identity object</returns>
        protected MsaSelfIdentity GetAndValidateIdentity(string userId)
        {
            bool oboAuth;
            switch (userId.ToUpperInvariant())
            {
                case "ME":
                case "MY":
                    oboAuth = false;
                    break;
                case "OBO":
                    oboAuth = true;
                    break;
                default:
                    throw new SecurityException("Must use 'me' or 'my' for userId.");
            }

            var identity = this.User.Identity as MsaSelfIdentity;
            if (identity == null)
            {
                throw new SecurityException("Identity not found in User context.");
            }

            if (!identity.IsAuthenticated)
            {
                throw new SecurityException("Identity is not marked as authenticated.");
            }

            if (oboAuth && identity.AuthorizingPuid == identity.TargetPuid)
            {
                throw new SecurityException("OBO auth must have family token with child target.");
            }

            return identity;
        }
    }
}
