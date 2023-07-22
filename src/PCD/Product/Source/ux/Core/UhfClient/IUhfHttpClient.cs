using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.UhfClient
{
    /// <summary>
    /// Interface for HTTP client used by UHF client.
    /// </summary>
    public interface IUhfHttpClient
    {
        /// <summary>
        /// Gets configured HTTP client instance.
        /// </summary>
        HttpClient HttpClient { get; }
    }
}
