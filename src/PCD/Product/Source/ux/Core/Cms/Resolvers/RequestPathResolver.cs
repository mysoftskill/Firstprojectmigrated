// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using Cms.Web.Core;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Resolvers
{
    /// <summary>
    /// This is required by the Compass SDK. Simply implement interface and return string.empty to appease it.
    /// </summary>
    public class RequestPathResolver : IRequestPathProvider
    {
        /// <summary>
        /// This function is the implementation of the IRequestPathProvider
        /// </summary>
        /// <returns>empty string as this is required by the Compass SDK but can be ignored.</returns>
        public string GetRequestPath()
        {
            return string.Empty;
        }
    }
}