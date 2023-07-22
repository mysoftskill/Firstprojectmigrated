// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;

    /// <summary>
    /// A WCF executor pipelined component which handles updating performance counters.
    /// </summary>
    public class PerfCounterWcfHandler : DelegatingWcfHandler
    {
        private ICounterFactory counterFactory;

        private string componentName;

        private string methodName;

        public PerfCounterWcfHandler(
            ICounterFactory counterFactory,
            string componentName,
            string methodName)
        {
            this.counterFactory = counterFactory;
            this.componentName = componentName;
            this.methodName = methodName;
        }

        public async override Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            // Execution helper internally updates performance counters
            TimedOperationExecutionResult<T> result =
                await RequestExecutionHelper.ExecuteTimedActionAsync<T>(
                counterFactory: this.counterFactory,
                componentName: this.componentName,
                methodName: this.methodName,
                action: async () => await base.ExecuteAsync(action));

            if (!result.IsSuccess)
            {
                throw result.Exception;
            }

            return result.Response;
        }

        public override async Task ExecuteAsync(Func<Task> action)
        {
            // Execution helper internally updates performance counters
            TimedOperationExecutionResult result =
                await RequestExecutionHelper.ExecuteTimedActionAsync(
                counterFactory: this.counterFactory,
                componentName: this.componentName,
                methodName: this.methodName,
                action: async () => await base.ExecuteAsync(action));

            if (!result.IsSuccess)
            {
                throw result.Exception;
            }
        }
    }
}
