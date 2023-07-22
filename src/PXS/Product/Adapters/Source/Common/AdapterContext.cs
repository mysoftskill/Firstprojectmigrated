// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.PrivacyServices.Common.Azure;

    public class AdapterContext : IAdapterContext
    {
        public AdapterContext()
        {
        }

        public AdapterContext(ILogger logger, ICounterFactory counterFactory, ICertificateProvider certificateProvider, IServiceInfo serviceInfo)
        {
            if (null == logger)
            {
                throw new ArgumentNullException("logger");
            }

            if (null == counterFactory)
            {
                throw new ArgumentNullException("counterFactory");
            }

            if (null == certificateProvider)
            {
                throw new ArgumentNullException("certificateProvider");
            }

            if (null == serviceInfo)
            {
                throw new ArgumentNullException("serviceInfo");
            }

            this.Logger = logger;
            this.CounterFactory = counterFactory;
            this.CertificateProvider = certificateProvider;
            this.ServiceInfo = serviceInfo;
        }

        public ILogger Logger { get; set; }

        public ICounterFactory CounterFactory { get; set; }

        public ICertificateProvider CertificateProvider { get; set; }

        public IServiceInfo ServiceInfo { get; set; }
    }
}
