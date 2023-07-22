// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    public class ServiceInfo : IServiceInfo
    {
        public string Version { get; set; }

        public string UserAgent { get; set; }
    }
}
