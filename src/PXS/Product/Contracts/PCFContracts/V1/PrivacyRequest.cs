// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.Contracts.V1
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    using Newtonsoft.Json;

    /// <summary>
    ///     Privacy Request
    /// </summary>
    public class PrivacyRequest
    {
        /// <summary>
        ///     Gets or sets the identifier of the authenticated and authorized user who performed the delete/export action.
        /// </summary>
        /// <remarks>
        ///     For authorization id represented as
        ///     1. A puid, this is prefixed with p:.
        ///     2. An AAD object Id, this is prefixed with a:.
        ///     3. A 128-bit hash of domain\user name, represented as a GUID string w:.
        ///     4. A Windows SID, this is prefixed with s:.
        ///     see https://osgwiki.com/wiki/CommonSchema/user_id for more
        /// </remarks>
        /// <example>p:123456789</example>
        [JsonProperty("authorizationId")]
        public string AuthorizationId { get; set; }

        /// <summary>
        ///     Gets or sets the cloud instance.
        /// </summary>
        /// <example>Public, China, Fairfax</example>
        [JsonProperty("cloudInstance", NullValueHandling = NullValueHandling.Ignore)]
        public string CloudInstance { get; set; }

        /// <summary>
        ///     Gets or sets the context of the command. This is state that flows through and is later visible in command status listings.
        /// </summary>
        [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
        public string Context { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the request is applicable to agents processing controller data
        /// </summary>
        /// <remarks>
        ///     this defaults to being applicable to controllers to ensure that accidentally not setting it explicitly does not
        ///     result in a failure to process relevant data correctly
        /// </remarks>
        [JsonProperty("controllerApplicable")]
        public bool ControllerApplicable { get; set; } = true;

        /// <summary>
        ///     Gets or sets the correlation vector.
        /// </summary>
        /// <remarks>
        ///     The cV is a light weight vector clock which can be used to identify and order related events across clients and services. It is supported by all Asimov
        ///     Instrumentation libraries as a Part A field: cV.
        /// </remarks>
        [JsonProperty("cv")]
        public string CorrelationVector { get; set; }

        /// <summary>
        ///     Indicates this is an end to end test request (should only be sent to known agents)
        /// </summary>
        [JsonProperty("isSynthetic", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsSyntheticRequest { get; set; }

        /// <summary>
        ///     Indicates the request is a test request.
        ///     This is defined as originating from an E2E test user or tenant that we control in an AllowedList.
        /// </summary>
        /// <example>
        ///     An E2E test for a real user or tenant that uses the real system (orignating from MS Graph) can execute automatically
        ///     at a scheduled interval for an AllowedList tenant id, and it could be tagged as a test request.
        /// </example>
        [JsonProperty("isTest", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsTestRequest { get; set; }

        /// <summary>
        ///     Indicates the request came from a watchdog
        /// </summary>
        [JsonProperty("isWatchdog", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsWatchdogRequest { get; set; }

        /// <summary>
        ///     The portal making the request.
        /// </summary>
        [JsonProperty("portal")]
        public string Portal { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the request is applicable to agents processing processor data
        /// </summary>
        /// <remarks>
        ///     this defaults to being applicable to processors to ensure that accidentally not setting it explicitly does not
        ///     result in a failure to process relevant data correctly
        /// </remarks>
        [JsonProperty("processorApplicable")]
        public bool ProcessorApplicable { get; set; } = true;

        /// <summary>
        ///     Gets or sets the requester of the command. This is a token that is used for filtering when asking PCF to return the status of issued commands, so that Boeing for example
        ///     doesn't see PCD issued requests in the listing.
        /// </summary>
        [JsonProperty("requester")]
        public string Requester { get; set; }

        /// <summary>
        ///     Gets or sets the request guid. This value uniquely identifies all requests that originated from the same user request batch. PXS will generate this value.
        /// </summary>
        /// <remarks>For example, a privacy dashboard user may request multiple single deletes as part of a single request. Those requests all would have the same request guid.</remarks>
        [JsonProperty("requestGuid")]
        public Guid RequestGuid { get; set; }

        /// <summary>
        ///     Gets or sets the request identifier.
        /// </summary>
        /// <remarks>An unique identifier represented a request. This is also referred to as the command id.</remarks>
        [JsonProperty("requestId")]
        [JsonRequired]
        public Guid RequestId { get; set; }

        /// <summary>
        ///     Gets or sets the type of the request.
        /// </summary>
        /// <example>Export, Delete, AccountClose</example>
        [JsonProperty("requestType")]
        public RequestType RequestType { get; set; }

        /// <summary>
        ///     Gets or sets the subject. This property is not directly serialized; instead, it is invoked through the "RawSubject" property.
        /// </summary>
        /// <example>MsaUser</example>
        [JsonProperty("subject")]
        public IPrivacySubject Subject { get; set; }

        /// <summary>
        ///     Gets or sets the timestamp for when the user initiated the request.
        /// </summary>
        [JsonProperty("timestamp")]
        [JsonRequired]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        ///     Gets or sets the v2 verification token for this request.
        /// </summary>
        /// <remarks>
        ///     The v2 verification token is a signature attached to the command used by receiving agents to verify integrity.
        /// </remarks>
        [JsonProperty("verificationToken")]
        public string VerificationToken { get; set; }

        /// <summary>
        ///     Gets or sets the v3 verification token for this request.
        /// </summary>
        /// <remarks>
        ///     The v3 verification token is a signature attached to the command used by receiving agents to verify integrity.
        /// </remarks>
        [JsonProperty("verificationTokenV3")]
        public string VerificationTokenV3 { get; set; }

        /// <summary>
        ///     Performs a shallow copy of the DeleteRequestV1 object.
        /// </summary>
        /// <returns>A shallow copy of the input PrivacyRequest.</returns>
        public PrivacyRequest ShallowCopyWithNewId()
        {
            var returnValue = (PrivacyRequest)this.MemberwiseClone();
            returnValue.RequestId = Guid.NewGuid();
            return returnValue;
        }
    }
}
