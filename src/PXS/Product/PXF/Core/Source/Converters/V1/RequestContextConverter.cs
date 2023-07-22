// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Converters
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;

    /// <summary>
    ///     RequestContext Converter
    /// </summary>
    public static class RequestContextConverter
    {
        /// <summary>
        ///     Converts the <see cref="IRequestContext" /> to <see cref="PrivacyAdapters.IPxfRequestContext" />.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>
        ///     <see cref="PrivacyAdapters.IPxfRequestContext" />
        /// </returns>
        public static IPxfRequestContext ToAdapterRequestContext(this IRequestContext requestContext)
        {
            string mergedTicket = null;
            switch (requestContext.Identity)
            {
                case AadIdentityWithMsaUserProxyTicket aadWithMsa:
                    mergedTicket = aadWithMsa.UserProxyTicket;
                    break;
                case AadIdentity aad:
                    mergedTicket = aad.AccessToken;
                    break;
                case MsaSelfIdentity msa:
                    mergedTicket = msa.UserProxyTicket;
                    break;
            }

            return new PxfRequestContext(
                mergedTicket,
                requestContext.GetIdentityValueOrDefault<MsaSelfIdentity, string>(i => i.FamilyJsonWebToken),
                requestContext.GetIdentityValueOrDefault<MsaSelfIdentity, long>(i => i.AuthorizingPuid),
                requestContext.TargetPuid,
                requestContext.TargetCid,
                requestContext.GetIdentityValueOrDefault<MsaSelfIdentity, string>(i => i.TargetCountry),
                requestContext.IsWatchdogRequest,
                requestContext.Flights);
        }
    }
}
