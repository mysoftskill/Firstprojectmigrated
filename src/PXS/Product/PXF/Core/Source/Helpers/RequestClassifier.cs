// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     Classifies requests based on the identity of the authenticated user.
    /// </summary>
    public class RequestClassifier : IRequestClassifier
    {
        private readonly HashSet<string> AllowedListCorrelationContextBaseOperationNames;

        private readonly HashSet<Guid> AllowedListAadObjectIds;

        private readonly HashSet<Guid> AllowedListAadTenantIds;

        private readonly HashSet<long> AllowedListMsaPuids;

        /// <summary>
        ///     Creates a new instance of RequestClassifier
        /// </summary>
        /// <param name="privacyConfigurationManager"></param>
        public RequestClassifier(IPrivacyConfigurationManager privacyConfigurationManager)
        {
            ITestRequestClassifierConfiguration config = privacyConfigurationManager?.PrivacyExperienceServiceConfiguration?.TestRequestClassifierConfig;

            this.AllowedListAadTenantIds = new HashSet<Guid>();
            this.AllowedListAadObjectIds = new HashSet<Guid>();
            this.AllowedListMsaPuids = new HashSet<long>();
            this.AllowedListCorrelationContextBaseOperationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (config != null)
            {
                this.AllowedListAadTenantIds.UnionWith(config.AllowedListAadTenantIds.Select(Guid.Parse));
                this.AllowedListAadObjectIds.UnionWith(config.AllowedListAadObjectIds.Select(Guid.Parse));
                this.AllowedListMsaPuids.UnionWith(config.AllowedListMsaPuids);
                this.AllowedListCorrelationContextBaseOperationNames.UnionWith(config.CorrelationContextBaseOperationNames);
            }
        }

        /// <inheritdoc />
        public bool IsTestRequest(string requestOriginPortal, IIdentity identity, string correlationContextBaseOperationName = null)
        {
            if (requestOriginPortal == Portals.PartnerTestPage)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(correlationContextBaseOperationName) &&
                this.AllowedListCorrelationContextBaseOperationNames.Contains(correlationContextBaseOperationName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            // The authenticated user is used to determine if the request is classified as test or not. The target user is not considered, by design.
            switch (identity)
            {
                case AadIdentityWithMsaUserProxyTicket aadIdentityWithMsaUserProxyTicket:

                    // For this identity, the proxy ticket contains the target puid, but the object id is the user who is signed in.
                    // Thus test request is based on the object id or tenant id of the authenticated AAD user.
                    return this.AllowedListAadObjectIds.Contains(aadIdentityWithMsaUserProxyTicket.ObjectId) ||
                           this.AllowedListAadTenantIds.Contains(aadIdentityWithMsaUserProxyTicket.TenantId);

                case AadIdentity aadIdentity:
                    return this.AllowedListAadTenantIds.Contains(aadIdentity.TenantId) || this.AllowedListAadObjectIds.Contains(aadIdentity.ObjectId);

                case MsaSelfIdentity msaSelfIdentity:
                    return this.AllowedListMsaPuids.Contains(msaSelfIdentity.AuthorizingPuid);
            }

            return false;
        }
    }
}
