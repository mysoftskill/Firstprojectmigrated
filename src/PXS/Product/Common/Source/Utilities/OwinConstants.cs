// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    public static class OwinConstants
    {
        /// <summary>
        /// A wildcard for any IPv4 address. Use when starting up the web app to listen on any address within a specific port.
        /// https://github.com/danesparza/OWIN-WebAPI-Service#serving-more-than-just-localhost
        /// </summary>
        public const string IPv4Wildcard = "+"; 
    }
}
