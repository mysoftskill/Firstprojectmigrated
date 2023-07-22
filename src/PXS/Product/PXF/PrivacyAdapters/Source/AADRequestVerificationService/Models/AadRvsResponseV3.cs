// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models
{
    using Newtonsoft.Json;

    /// <summary>
    ///     AAD RVS response model.
    ///     https://microsoft.sharepoint.com/:w:/t/DataScienceEngineering/ES6eeH2YQnFDqNF94ai0k90BbUxEJCsEjBcGtN9v34_RTg?e=MvhfZe
    /// </summary>
    public class AadRvsResponseV3 : AadRvsResponse
    {
        /// <summary>
        /// An array of V3 verifiers
        /// </summary>
        [JsonProperty("verifiers")]
        public string[] Verifiers;

        /// <summary>
        /// An opaque string that is optionally present in the response body. This indicates that the verifiers array is incomplete and 
        /// that the call needs to be made again specifying this value in the request header as “x-ms-continuation”
        /// </summary>
        [JsonProperty("continuation")]
        public string Continuation;
    }
}
