// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;
    using System.Collections.Specialized;
    using System.Text;
    using System.Web;

    /// <summary>
    ///     Helpful extensions for dealing with <see cref="IResourceSource{T}" />.
    /// </summary>
    public static class ResourceSourceExtensions
    {
        /// <summary>
        ///     Given a <see cref="ResourceSourceContinuationToken" /> and a <see cref="Uri" /> this method will
        ///     non-destructively rewrite the url to contain the continuation token. This only replaces or adds the
        ///     <paramref name="nextTokenParamName" /> parameter on the url
        /// </summary>
        public static Uri BuildNextLink(this ResourceSourceContinuationToken token, Uri currentUri, string nextTokenParamName)
        {
            // Serialize ourselves
            string nextToken = token.Serialize();

            // Parse the current query parameters
            var uriBuilder = new UriBuilder(currentUri);
            NameValueCollection queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);

            // Replace or add the next token
            queryParams[nextTokenParamName] = nextToken;

            // Rebuild the query parameters
            var query = new StringBuilder();
            foreach (string param in queryParams)
            {
                if (query.Length > 0)
                    query.Append("&");

                query.Append(HttpUtility.UrlEncode(param));
                query.Append("=");
                query.Append(HttpUtility.UrlEncode(queryParams[param]));
            }
            uriBuilder.Query = query.ToString();

            // Return the modified uri
            return uriBuilder.Uri;
        }

        /// <summary>
        ///     A helper method that handles deserializing the token as well as setting it. See <see cref="IResourceSource{T}.SetNextToken" />.
        /// </summary>
        public static void SetNextToken<T>(this IResourceSource<T> source, string nextToken) where T : class
        {
            source.SetNextToken(ResourceSourceContinuationToken.Deserialize(nextToken));
        }
    }
}
