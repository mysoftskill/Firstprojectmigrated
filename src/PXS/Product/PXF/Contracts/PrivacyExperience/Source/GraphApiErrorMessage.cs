// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    /// <summary>
    ///     GraphApiErrorMessages
    /// </summary>
    /// <remarks>Graph has standard error messages we use in the generic/default cases, refer to: <c>https://developer.microsoft.com/en-us/graph/docs/concepts/errors</c></remarks>
    public static class GraphApiErrorMessage
    {
        /// <summary>
        ///     Forbidden Default
        /// </summary>
        public const string ForbiddenDefault = "Access is denied to the requested resource. The user might not have enough permission.";

        /// <summary>
        ///     InternalServerError
        /// </summary>
        public const string InternalServerError = "There was an internal server error while processing the request.";

        /// <summary>
        ///     InvalidClientCredentials
        /// </summary>
        public const string InvalidClientCredentials = "The client request contained invalid credentials.";

        /// <summary>
        ///     InvalidInput
        /// </summary>
        public const string InvalidInputDefault = "Cannot process the request because it is malformed or incorrect.";

        /// <summary>
        ///     InvalidObjectIdFormat
        /// </summary>
        public const string InvalidObjectIdFormat = "The specified object-id: {0} in the request uri is an invalid Guid.";

        /// <summary>
        ///     InvalidTenantIdFormat
        /// </summary>
        public const string InvalidTenantIdFormat = "The specified tenant-id: {0} in the request uri is an invalid Guid.";

        /// <summary>
        ///     MissingHeaderFormat
        /// </summary>
        public const string MissingHeaderFormat = "The request is missing a required header: {0}";

        /// <summary>
        ///     OperationNotFound
        /// </summary>
        public const string OperationNotFound = "Cannot find any operations with the input Id.";

        /// <summary>
        ///     PartnerTimeout
        /// </summary>
        public const string PartnerTimeout = "A dependency request timed out.";

        /// <summary>
        ///     ResourceNotFound
        /// </summary>
        public const string ResourceNotFound = "The resource is not found.";

        /// <summary>
        ///     SharedAccessSignatureTokenInvalid
        /// </summary>
        public const string SharedAccessSignatureTokenInvalid = "Storage destination does not include SAS credentials.";

        /// <summary>
        ///     StorageLocationAlreadyUsed
        /// </summary>
        public const string StorageLocationAlreadyUsed =
            "Storage destination has already been used for an export. If this is not true, delete 'RequestInfo.json' from the container and try again.";

        /// <summary>
        ///     StorageLocationInvalid
        /// </summary>
        public const string StorageLocationInvalid = "Storage Location is not valid.";

        /// <summary>
        ///     StorageLocationNeedsWriteAddPermissions
        /// </summary>
        public const string StorageLocationNeedsWriteAddPermissions = "Storage destination does not have write/add permissions.";

        /// <summary>
        ///     StorageLocationNotAzureBlob
        /// </summary>
        public const string StorageLocationNotAzureBlob = "Storage destination must be an azure blob container.";

        /// <summary>
        ///     StorageLocationNotServiceSAS
        /// </summary>
        public const string StorageLocationNotServiceSAS = "Storage destination needs to have a Service SAS, not an Account SAS. See " +
                                                           "https://docs.microsoft.com/en-us/azure/storage/common/storage-dotnet-shared-access-signature-part-1 for more information.";

        /// <summary>
        ///     StorageLocationShouldNotAllowListAccess
        /// </summary>
        public const string StorageLocationShouldNotAllowListAccess = "Storage destination should not allow list access.";

        /// <summary>
        ///     StorageLocationShouldNotAllowRead
        /// </summary>
        public const string StorageLocationShouldNotAllowReadAccess = "Storage destination should not allow read access.";

        /// <summary>
        ///     StorageLocationShouldSupportAppendBlobs
        /// </summary>
        public const string StorageLocationShouldSupportAppendBlobs = "Storage destination should support append blobs. You might be using premium storage. See " +
                                                                      "https://docs.microsoft.com/en-us/rest/api/storageservices/using-blob-service-operations-with-azure-premium-storage#premium-storage-accounts-support-page-blobs-only for more information.";

        /// <summary>
        ///     TooManyRequests
        /// </summary>
        public const string TooManyRequests = "Client application has been throttled and should not attempt to repeat the request until an amount of time has elapsed.";

        /// <summary>
        ///     Unauthorized
        /// </summary>
        public const string Unauthorized = "Required authentication information is either missing or not valid for the resource.";

        /// <summary>
        ///     Unknown Default
        /// </summary>
        public const string UnknownDefault = "An unknown error occurred.";
    }
}
