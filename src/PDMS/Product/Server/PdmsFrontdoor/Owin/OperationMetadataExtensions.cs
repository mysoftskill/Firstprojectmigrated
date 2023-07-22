namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin
{
    using System.Linq;
    using Microsoft.Owin;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;

    /// <summary>
    /// Extension functions for the OperationMetadata class.
    /// </summary>
    public static class OperationMetadataExtensions
    {
        /// <summary>
        /// Fills in the metadata from the OWIN request and response.
        /// </summary>
        /// <param name="data">The metadata object.</param>
        /// <param name="response">The response.</param>
        /// <returns>The same metadata object.</returns>
        public static OperationMetadata FillForOwin(this OperationMetadata data, IOwinResponse response)
        {
            data.ProtocolStatusCode = (int)response.StatusCode;
            data.ResponseContentType = response.ContentType;

            return data;
        }

        /// <summary>
        /// Fills in the metadata from the OWIN request.
        /// </summary>
        /// <param name="data">The metadata object.</param>
        /// <param name="request">The request.</param>
        /// <returns>The same metadata object.</returns>
        public static OperationMetadata FillForOwin(this OperationMetadata data, IOwinRequest request)
        {
            data.CallerIpAddress = request.RemoteIpAddress ?? string.Empty;
            data.Protocol = request.Protocol;
            data.RequestMethod = request.Method;
            data.TargetUri = request.Uri.AbsoluteUri;
                       
            if (request.Headers != null && request.Headers.ContainsKey("Content-Length"))
            {
                data.RequestSizeBytes = int.Parse(request.Headers["Content-Length"]);
            }
            else
            {
                data.RequestSizeBytes = -1;
            }

            // Get and parse Correlation Context to avoid dependency on Sll. Sample Correlation-Context header format: "v=1,ms.b.tel.scenario=ust.privacy.export,ms.b.tel.partner=account.mscom.web".
            if (request.Headers != null && request.Headers.TryGetValue("Correlation-Context", out string[] correlationContextValues) && correlationContextValues.Any())
            {
                string cc = correlationContextValues[0];
                data.CC = cc.Split(',').Select(part => part.Split('=')).Where(part => part.Length == 2).Skip(1).ToDictionary(key => key[0], value => value[1]);
            }

            return data;
        }
    }
}
