// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models
{
    using Newtonsoft.Json;

    /// <summary>
    ///     AAD RVS actor request.
    /// </summary>
    public class AadRvsActorRequest
    {
        /// <summary>
        ///     Gets or sets the target tenant ID.
        /// </summary>
        public string TargetTenantId { get; set; }

        /// <summary>
        ///     Gets or sets the target object ID.
        /// </summary>
        public string TargetObjectId { get; set; }

        /// <summary>
        ///     Gets or sets the correlation ID.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        ///    Gets or sets the command ids.
        ///    rids – a set of 0 or more Guid values each separated by ',' or ';' or ':'.
        ///    Under replay scenario rids will be confirmed to be the same as the original verifier 
        ///    and recycled based on the original verifier.
        ///    Limit of up to 2048 bytes or 1024 characters. 
        /// </summary>
        [JsonProperty("rids", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string CommandIds { get; set; }
    }
}
