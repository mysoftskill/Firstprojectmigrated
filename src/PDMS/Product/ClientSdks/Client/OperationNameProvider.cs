namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Converts request information into friendly names.
    /// </summary>
    public static class OperationNameProvider
    {
        private static readonly IEnumerable<OperationDataProvider> OperationDataProviders;

        /// <summary>
        /// Initializes static members of the <see cref="OperationNameProvider" /> class.
        /// </summary>
        static OperationNameProvider()
        {
            var versions = Enum.GetValues(typeof(OperationDataVersion)) as OperationDataVersion[];

            OperationDataProviders = versions.Select(version => new OperationDataProvider(version));
        }

        /// <summary>
        /// Enumerates through all operation data values and finds the first one that matches the given path and query.
        /// If no match is found, then a fallback value is returned.
        /// </summary>
        /// <param name="httpMethod">The http method of the request.</param>
        /// <param name="pathAndQuery">The path and query of an URI.</param>
        /// <returns>The operation name.</returns>
        public static string GetFromPathAndQuery(string httpMethod, string pathAndQuery)
        {
            foreach (var provider in OperationDataProviders)
            {
                var data = provider.GetFromPathAndQuery(httpMethod, pathAndQuery);

                if (data != null)
                {
                    return data.Name;
                }
            }

            throw new ArgumentException($"Unrecognized pathAndQuery value {pathAndQuery} for httpMethod {httpMethod}.");
        }
    }
}