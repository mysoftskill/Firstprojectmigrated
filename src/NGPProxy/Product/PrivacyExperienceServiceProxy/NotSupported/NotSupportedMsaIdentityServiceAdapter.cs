// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PrivacyExperienceServiceProxy
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService;

    /// <summary>
    ///     NotSupported instance of Msa Identity Service Adapter
    /// </summary>
    public class NotSupportedMsaIdentityServiceAdapter : IMsaIdentityServiceAdapter
    {
        /// <inheritdoc />
        public Task<AdapterResponse<string>> GetGdprAccountCloseVerifierAsync(Guid commandId, long puid, string preVerifierToken, string xuid = "")
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<string>> GetGdprDeviceDeleteVerifierAsync(Guid commandId, long globalDeviceId, string predicate = "")
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<string>> GetGdprExportVerifierAsync(Guid commandId, IPxfRequestContext requestContext, Uri storageDestination, string xuid)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<string>> GetGdprUserDeleteVerifierAsync(IList<Guid> commandIds, IPxfRequestContext requestContext, string xuid = "", string predicate = "", string dataType = "")
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<IProfileAttributesUserData>> GetProfileAttributesAsync(IPxfRequestContext requestContext, params ProfileAttribute[] attributes)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<ISigninNameInformation>> GetSigninNameInformationAsync(long puid)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<IEnumerable<ISigninNameInformation>>> GetSigninNameInformationsAsync(IEnumerable<long> puids)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<string>> GetGdprUserDeleteVerifierWithPreverifierAsync(Guid commandId, IPxfRequestContext requestContext, string preVerifier, string xuid = "", string predicate = "", string dataType = "")
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<string>> GetGdprUserDeleteVerifierWithRefreshClaimAsync(IPxfRequestContext requestContext)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<string>> RenewGdprUserDeleteVerifierUsingPreverifierAsync(IPxfRequestContext requestContext, string preVerifier)
        {
            throw new NotSupportedException();
        }
    }
}
