// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service
{
    using System.Collections.Generic;
    using System.Net;

    using PXS = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    ///     HttpStatusCode Mapping maps <see cref="PXS.ErrorCode" /> to <see cref="HttpStatusCode" />
    /// </summary>
    public static class HttpStatusCodeMapping
    {
        /// <summary>
        ///     The <see cref="PXS.ErrorCode" /> to <see cref="HttpStatusCode" /> mapping.
        /// </summary>
        public static readonly Dictionary<PXS.ErrorCode, HttpStatusCode> Mapping =
            new Dictionary<PXS.ErrorCode, HttpStatusCode>
            {
                { PXS.ErrorCode.Unknown, HttpStatusCode.InternalServerError },
                { PXS.ErrorCode.PartnerError, HttpStatusCode.BadGateway },
                { PXS.ErrorCode.InvalidInput, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.MissingClientCredentials, HttpStatusCode.Unauthorized },
                { PXS.ErrorCode.InvalidClientCredentials, HttpStatusCode.Forbidden },
                { PXS.ErrorCode.PreconditionFailed, HttpStatusCode.PreconditionFailed },
                { PXS.ErrorCode.UnauthorizedStatutoryAge, HttpStatusCode.Unauthorized },
                { PXS.ErrorCode.UnauthorizedMajorityAge, HttpStatusCode.Unauthorized },
                { PXS.ErrorCode.PartnerTimeout, HttpStatusCode.GatewayTimeout },
                { PXS.ErrorCode.TooManyRequests, (HttpStatusCode)429 },
                { PXS.ErrorCode.Unauthorized, HttpStatusCode.Unauthorized },
                { PXS.ErrorCode.UpdateConflict, HttpStatusCode.Conflict },
                { PXS.ErrorCode.ResourceNotModified, HttpStatusCode.NotModified },
                { PXS.ErrorCode.CreateConflict, HttpStatusCode.Conflict },
                { PXS.ErrorCode.ResourceNotFound, HttpStatusCode.NotFound },
                { PXS.ErrorCode.TimeWindowExpired, HttpStatusCode.Unauthorized },
                { PXS.ErrorCode.Forbidden, HttpStatusCode.Forbidden },
                { PXS.ErrorCode.SharedAccessSignatureTokenInvalid, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.StorageLocationInvalid, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.StorageLocationShouldNotAllowListAccess, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.StorageLocationNeedsWriteAddPermissions, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.StorageLocationAlreadyUsed, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.StorageLocationNotAzureBlob, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.StorageLocationNotServiceSAS, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.StorageLocationShouldNotAllowReadAccess, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.StorageLocationShouldSupportAppendBlobs, HttpStatusCode.BadRequest },
                { PXS.ErrorCode.MethodNotAllowed, HttpStatusCode.MethodNotAllowed },
                { PXS.ErrorCode.ConcurrencyConflict, HttpStatusCode.Conflict }
            };
    }
}
