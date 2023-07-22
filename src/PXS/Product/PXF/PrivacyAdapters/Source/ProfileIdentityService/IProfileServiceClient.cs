// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService
{
    using System.Threading.Tasks;

    /// <summary>
    ///     Interface for accessing the MSA Profile Service
    /// </summary>
    /// <remarks>
    ///     https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/webframe.html
    /// </remarks>
    public interface IProfileServiceClient
    {
        /// <summary>
        ///     Gets the profile attributes
        /// </summary>
        /// <param name="puid">The Puid as a hexadecimal string</param>
        /// <param name="attributeList">
        ///     List of attributes to grab "*" for all defined attributes
        ///     see:
        ///     https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/Profile_GetProfileByAttribute.html
        /// </param>
        /// <returns>XML block with the retrieved data</returns>
        Task<string> GetProfileByAttributesAsync(string puid, string attributeList);
    }
}
