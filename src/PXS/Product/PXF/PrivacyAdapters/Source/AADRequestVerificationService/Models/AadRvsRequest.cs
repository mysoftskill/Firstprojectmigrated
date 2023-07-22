// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Aad Rvs Request Body - GDPR 2.0
    ///     https://microsoft.sharepoint.com/:w:/t/DataScienceEngineering/ETThJvxBjyhPkpZjxbH8JuUBRoWCSekpQaB9GT-J6Tj7Pg?e=xxonkD
    /// </summary>
    public class AadRvsRequest
    {
        /// <summary>
        ///     Gets or sets tenant id.
        /// </summary>
        [JsonProperty("tid")]
        public string TenantId { get; set; }

        /// <summary>
        ///     Gets or sets object id.
        /// </summary>
        [JsonProperty("oid")]
        public string ObjectId { get; set; }

        /// <summary>
        ///     Gets or sets storage path. Only applicable to export requests.
        /// </summary>
        [JsonProperty("azsp", NullValueHandling = NullValueHandling.Ignore)]
        public string StoragePath { get; set; }

        /// <summary>
        ///     Gets or sets operation.
        /// </summary>
        [JsonProperty("op")]
        public string Operation { get; set; }

        /// <summary>
        ///     Gets or sets  controller applicable flag.
        /// </summary>
        [JsonProperty("ca")]
        public bool ControllerApplicable { get; set; }

        /// <summary>
        ///     Gets or sets processor applicable flag.
        /// </summary>
        [JsonProperty("pa")]
        public bool ProcessorApplicable { get; set; }

        /// <summary>
        ///     Gets or sets correlation id.
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        /// <summary>
        ///     Gets or sets commandIds.
        /// </summary>
        [JsonProperty("rids")]
        public string CommandIds { get; set; }

        /// <summary>
        ///     Gets or sets Org Id Puid. Only applicable for account close.
        /// </summary>
        [JsonProperty("puid", NullValueHandling = NullValueHandling.Ignore)]
        public string OrgIdPuid { get; set; }

        /// <summary>
        ///     Gets or sets predicate. Optional.
        /// </summary>
        [JsonProperty("pred", NullValueHandling = NullValueHandling.Ignore)]
        public string Predicate { get; set; }

        /// <summary>
        ///     Gets or sets pre-verifier. Only applicable for account close.
        /// </summary>
        [JsonProperty("preverif", NullValueHandling = NullValueHandling.Ignore)]
        public string PreVerifier { get; set; }
    }
}
