// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    /// A WCF executor pipelined component which handles setting Service Point properties for each request.
    /// </summary>
    public class ServicePointWcfHandler : DelegatingWcfHandler
    {
        private IServicePointConfiguration configuration;
        private ICounterFactory counterFactory;
        private Uri endpointAddress;

        /// <summary>
        /// Creates a ServicePoint WCF Handler that pulls the Service Point configuration values
        /// from the provided configuration object.
        /// </summary>
        /// <param name="configuration">Configuration object used to set the values.</param>
        /// <param name="endpointAddress">Address of endpoint whose Service Point object needs to be updated.</param>
        /// <param name="counterFactory">Counter factory to create Service Point specific counters.</param>
        public ServicePointWcfHandler(IServicePointConfiguration configuration, Uri endpointAddress, ICounterFactory counterFactory)
        {
            this.configuration = configuration;
            this.endpointAddress = endpointAddress;
            this.counterFactory = counterFactory;
        }

        /// <summary>
        /// Execute a WCF operation and set the ServicePoint configuration values.
        /// </summary>
        /// <typeparam name="T">Specifies the contract type the WCF operation returns.</typeparam>
        /// <param name="action">A function which executes a WCF operation.</param>
        /// <returns>A asynchronous task executing the delegate.</returns>
        public override async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(this.endpointAddress);
            SetServicePointProperties(servicePoint);
            
            T result = await base.ExecuteAsync(action);

            PerfCounterHelper.UpdateHttpConnectionCountCounter(counterFactory, servicePoint);

            return result;
        }

        /// <summary>
        /// Execute a WCF operation and set the ServicePoint configuration values.
        /// </summary>
        /// <param name="action">A function which executes a WCF operation.</param>
        /// <returns>A asynchronous task executing the delegate.</returns>
        public override async Task ExecuteAsync(Func<Task> action)
        {
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(this.endpointAddress);
            SetServicePointProperties(servicePoint);
            
            await base.ExecuteAsync(action);

            PerfCounterHelper.UpdateHttpConnectionCountCounter(counterFactory, servicePoint);
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
