// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers.Helpers
{
    using System.Collections.Generic;
    using System.Security.Principal;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    internal class PortalHelper
    {
        /// <remarks>
        ///     This method is NOT general purpose. It used to be private for a specific purpose and was extracted out to a static
        ///     here. If you are adding new usages of this method, or moving existing usages around, please make sure you make sure this
        ///     will work or not for your case, as well as that you don't break any existing cases.
        /// </remarks>
        internal static string DeducePortal(ApiController apiController, IIdentity identity, IDictionary<string, string> siteIdToCallerName)
        {
            if (identity is AadIdentity aadIdentity)
            {
                if (siteIdToCallerName.TryGetValue(aadIdentity.ApplicationId, out string siteName))
                {
                    if (siteName == "PCD_PPE" || siteName == "PCD_PROD")
                        return Portals.Pcd;

                    return $"{Portals.Unknown}_{aadIdentity.ApplicationId}_{siteName}";
                }

                return $"{Portals.Unknown}_{aadIdentity.ApplicationId}";
            }

            if (identity is MsaSiteIdentity msaIdentity)
            {
                if (siteIdToCallerName.TryGetValue(msaIdentity.CallerMsaSiteId.ToString(), out string siteName))
                {
                    if (siteName == "MEEPortal_INT_PROD" || siteName == "MEEPortal_PPE" && apiController != null)
                    {
                        if (apiController is TimelineV2Controller)
                            return Portals.Amc;
                        if (apiController is PrivacyRequestApiController)
                            return Portals.PartnerTestPage;
                    }

                    return $"{Portals.Unknown}_{msaIdentity.CallerMsaSiteId}_{siteName}";
                }

                return $"{Portals.Unknown}_{msaIdentity.CallerMsaSiteId}";
            }

            return Portals.Unknown;
        }
    }
}
