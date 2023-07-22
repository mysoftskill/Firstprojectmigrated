// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Graph
{
    using Newtonsoft.Json;

    /// <summary>
    ///     DirectoryRole.
    /// </summary>
    public class DirectoryRole
    {
        /// <summary>
        ///     Gets or sets the ObjectId.
        /// </summary>
        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        /// <summary>
        ///     Gets or sets the RoleTemplateId.
        /// </summary>
        [JsonProperty("roleTemplateId")]
        public string RoleTemplateId { get; set; }

        /// <summary>
        ///     Gets or sets the DisplayName.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
