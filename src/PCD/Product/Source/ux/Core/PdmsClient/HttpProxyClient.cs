using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.DataManagement.Client;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    public sealed class HttpProxyClient : BaseHttpServiceProxy
    {
        public HttpProxyClient(HttpClient httpClient) : base(httpClient)
        {
        }
    }
}
