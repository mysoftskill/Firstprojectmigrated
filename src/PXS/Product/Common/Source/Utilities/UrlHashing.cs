// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    public class UrlHashing
    {
        /// <summary>
        ///     Hashes a Url by first normalizing, then MD5 hashing the result, and converting to hex string
        ///     Base64 is maybe not a great solution since the hash is always the same length, so the two
        ///     trailing padding characters are annoying. Base64 is also not the most Url-Friendly format
        ///     Hex string is a little bit longer since it's not as compact (base 16 vs base 64) which
        ///     is also a bit annoying, but more url-friendly and not as complicated as a custom base64
        ///     implementation.
        /// </summary>
        public static string HashUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            string normalUrl = NormalizeUrl(url);
            using (HashAlgorithm sha256 = SHA256.Create()) //MD5 is obsolete, SHA256 is the recommended way now.
            {
                return string.Join(
                    string.Empty,
                    sha256.ComputeHash(Encoding.UTF8.GetBytes(normalUrl)).Select(b => b.ToString("x2"))); 
            }
        }

        /// <summary>
        ///     https://en.wikipedia.org/wiki/URL_normalization
        ///     Scheme/Host is made lowercase
        ///     The url is decoded (to get rid of casing differences in any encodings or + vs %20 differences)
        ///     Port 80/443 is removed if present and would be the default
        ///     Trailing slash normalization (only when there's no path)
        ///     We do not do any of the normalization that could change the semantics
        ///     This depends on UriBuilder behavior being the same everywhere, as well as WebUtility.UrlDecode
        ///     WebUtility should be more ubiquitous than HttpUtility and not require referencing System.Web but
        ///     they should behave identically.
        /// </summary>
        public static string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                var ub = new UriBuilder(uri);
                ub.Host = ub.Host.ToLowerInvariant();
                try
                {
                    ub.Host = new IdnMapping().GetUnicode(ub.Host);
                }
                catch (ArgumentException)
                {
                    // If IDN exception happens, just carry on without it
                }
                ub.Scheme = ub.Scheme.ToLowerInvariant();
                url = ub.Uri.ToString();
            }

            return WebUtility.UrlDecode(url);
        }
    }
}
