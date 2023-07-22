// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Security
{
    using System.Security.Principal;

    public class MsaSelfIdentity : IIdentity
    {
        public string AuthenticationType
        {
            get { return "MsaSelf"; }
        }

        public bool IsAuthenticated
        {
            get;
            internal set;
        }

        public string Name
        {
            get;
            internal set;
        }

        public long? AuthorizingPuid
        { 
            get;
            internal set;
        }

        public long? TargetPuid
        {
            get;
            internal set;
        }

        public long? SiteId
        {
            get;
            internal set;
        }
    }
}
