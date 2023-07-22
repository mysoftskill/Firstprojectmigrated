// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.Handlers
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    /// An HTTP client handler to set Service Point properties for each request.
    /// </summary>
    public class ServicePointHandler : DelegatingHandler
    {
        private IServicePointConfiguration configuration;
        private ICounterFactory counterFactory;

        /// <summary>
        /// Creates a ServicePoint Handler that pulls the Service Point configuration values
        /// from the provided configuration object.
        /// </summary>
        /// <param name="configuration">Configuration object used to set the values.</param>
        /// <param name="counterFactory">Counter factory to create Service Point specific counters.</param>
        public ServicePointHandler(IServicePointConfiguration configuration, ICounterFactory counterFactory)
        {
            this.configuration = configuration;
            this.counterFactory = counterFactory;
        }

        /// <summary>
        /// Updates the Service Point properties before handing off the request to the next handler.
        /// </summary>
        /// <param name="request">Request whose matching Service Point will be updated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Response message from the pipeline.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(request.RequestUri);
            SetServicePointProperties(servicePoint);

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            PerfCounterHelper.UpdateHttpConnectionCountCounter(counterFactory, servicePoint);

            return response;
        }

        private int ConnectionLeaseTimeout
        {
            get
            {
                return configuration.ConnectionLeaseTimeout;
            }
        }

        private int ConnectionLimit
        {
            get
            {
                return configuration.ConnectionLimit;
            }
        }

        private int MaxIdleTime
        {
            get
            {
                return configuration.MaxIdleTime;
            }
        }
        
        private void SetServicePointProperties(ServicePoint servicePoint)
        {
            servicePoint.ConnectionLeaseTimeout = ConnectionLeaseTimeout;
            servicePoint.ConnectionLimit = ConnectionLimit;
            servicePoint.MaxIdleTime = MaxIdleTime;
        }
    }
}
