// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.PrivacyServices.Common.Azure;

    public interface IAdapterContext
    {
        ICounterFactory CounterFactory { get; }

        ILogger Logger { get; }

        ICertificateProvider CertificateProvider { get; }

        IServiceInfo ServiceInfo { get; }
    }
}
