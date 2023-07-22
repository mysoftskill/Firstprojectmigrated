// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Graph
{
    using Newtonsoft.Json;

    /// <summary>
    ///     DirectoryRoleMember
    /// </summary>
    public class DirectoryRoleMember
    {
        /// <summary>
        ///     Gets or sets Url.
        ///     This API from AAD Graph does not return objectIds of the members, but URLs containing them.
        ///     https://msdn.microsoft.com/Library/Azure/Ad/Graph/api/directoryroles-operations#GetDirectoryRoleMembers
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
