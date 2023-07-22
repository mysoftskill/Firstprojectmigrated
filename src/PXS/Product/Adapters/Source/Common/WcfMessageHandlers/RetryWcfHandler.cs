// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A WCF executor pipelined component which handles retrying on errors.
    /// </summary>
    public class RetryWcfHandler : DelegatingWcfHandler
    {
        /// <summary>
        /// The retry strategy.
        /// </summary>
        private readonly RetryManager retryManager;

        /// <summary>
        /// The component name.
        /// </summary>
        private readonly string componentName;

        /// <summary>
        /// The method name.
        /// </summary>
        private readonly string methodName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryWcfHandler"/> class.
        /// </summary>
        /// <param name="errorDetectionStrategy">The error-detection strategy.</param>
        /// <param name="retryStrategyConfiguration">The retry-strategy configuration.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="componentName">The component name.</param>
        /// <param name="methodName">The method name.</param>
        public RetryWcfHandler(
            ITransientErrorDetectionStrategy errorDetectionStrategy,
            IRetryStrategyConfiguration retryStrategyConfiguration,
            ILogger logger,
            string componentName,
            string methodName)
        {
            // REVISIT(nnaemeka): create once and re-use.
            // component-name & method-name shouldn't be an instance method. 
            this.retryManager = new RetryManager(retryStrategyConfiguration, logger, errorDetectionStrategy);
            this.componentName = componentName;
            this.methodName = methodName;
        }

        /// <summary>
        /// Execute a WCF operation and retry using the specified error detection and retry strategy.
        /// </summary>
        /// <typeparam name="T">Specifies the contract type the WCF operation returns.</typeparam>
        /// <param name="action">A function which executes a WCF operation.</param>
        /// <returns>A asynchronous task executing the delegate.</returns>
        public override async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            return await this.retryManager.ExecuteAsync(this.componentName, this.methodName, () => base.ExecuteAsync(action));
        }

        /// <summary>
        /// Execute a WCF operation and retry using the specified error detection and retry strategy.
        /// </summary>
        /// <param name="action">A function which executes a WCF operation.</param>
        /// <returns>A asynchronous task executing the delegate.</returns>
        public override async Task ExecuteAsync(Func<Task> action)
        {
            await this.retryManager.ExecuteAsync(this.componentName, this.methodName, () => base.ExecuteAsync(action));
        }
    }
}
