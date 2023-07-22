namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// An implementation of IOperationNameProvider which handles all known versions of API data.
    /// </summary>
    public class OperationNameProvider : IOperationNameProvider
    {
        private readonly IEnumerable<OperationDataProvider> operationDataProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationNameProvider" /> class.
        /// </summary>
        public OperationNameProvider()
        {
            var versions = Enum.GetValues(typeof(OperationDataVersion)) as OperationDataVersion[];

            this.operationDataProviders = versions.Select(version => new OperationDataProvider(version));
        }

        /// <summary>
        /// Enumerates through all operation data values and finds the first one that matches the given path and query.
        /// If no match is found, then a fallback value is returned.
        /// </summary>
        /// <param name="httpMethod">The http method of the request.</param>
        /// <param name="pathAndQuery">The path and query of an URI.</param>
        /// <returns>The operation name.</returns>
        public OperationName GetFromPathAndQuery(string httpMethod, string pathAndQuery)
        {
            var decodedValue = WebUtility.UrlDecode(pathAndQuery);

            foreach (var provider in this.operationDataProviders)
            {
                var data = provider.GetFromPathAndQuery(httpMethod, decodedValue);

                if (data != null)
                {
                    return new OperationName { FriendlyName = data.Name, IncludeInTelemetry = !data.ExcludeFromTelemetry };
                }
            }

            return new OperationName { FriendlyName = "Unknown.Api", IncludeInTelemetry = true };
        }
    }
}