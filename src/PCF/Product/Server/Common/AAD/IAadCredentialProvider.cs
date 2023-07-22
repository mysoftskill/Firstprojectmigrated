namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Rest;

    /// <summary>
    /// An interface that can fetch AAD tokens.
    /// </summary>
    public interface IAadCredentialProvider
    {
        /// <summary>
        /// Gets token credentials for the given AAD resource.
        /// </summary>
        Task<TokenCredentials> GetCredentialsAsync(string resource);
    }
}
