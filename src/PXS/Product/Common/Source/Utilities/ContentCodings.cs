// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    /// <summary>
    /// Content codings defined in RFC 2616. Use these values in Accept-Encoding or Content-Encoding headers.
    /// https://tools.ietf.org/html/rfc2616#section-3.5
    /// </summary>
    public static class ContentCodings
    {
        // GNU zip format as defined by https://tools.ietf.org/html/rfc1952
        public const string GZip = "gzip";
    }
}
