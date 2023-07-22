// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Allows a delegate to be executed with retries on transient errors.
    /// </summary>
    public class RetryManager
    {
        private readonly ILogger logger;

        private readonly RetryPolicy retryPolicy;

        /// <summary>
        ///     Creates an instance of RetryManager
        /// </summary>
        /// <param name="retryStrategyConfiguration">Specifies retry strategy to be used when a transient error is encountered</param>
        /// <param name="logger">Used for Geneva tracing</param>
        /// <param name="errorDetectionStrategy">Error detection strategy</param>
        /// <param name="logErrors">true to log errors; false otherwise</param>
        public RetryManager(
            IRetryStrategyConfiguration retryStrategyConfiguration,
            ILogger logger,
            ITransientErrorDetectionStrategy errorDetectionStrategy,
            bool logErrors)
        {
            if (retryStrategyConfiguration != null)
            {
                RetryStrategy retryStrategy = GetRetryStrategy(retryStrategyConfiguration);

                if (retryStrategy != null)
                {
                    this.retryPolicy = new RetryPolicy(errorDetectionStrategy, retryStrategy);
                }
            }

            // this maintains compatibility with existing code that does not log retry errors...
            this.logger = logErrors ? logger : null;
        }

        /// <summary>
        ///     Creates an instance of RetryManager using WebTransientErrorDetectionStrategy.
        /// </summary>
        /// <param name="retryStrategyConfiguration">Specifies retry strategy to be used when a transient error is encountered.</param>
        /// <param name="logger">Used for tracing.</param>
        /// <param name="errorDetectionStrategy">Error detection strategy.</param>
        public RetryManager(
            IRetryStrategyConfiguration retryStrategyConfiguration,
            ILogger logger,
            ITransientErrorDetectionStrategy errorDetectionStrategy)
            :
            this(retryStrategyConfiguration, logger, errorDetectionStrategy, false)
        {
        }

        /// <summary>
        ///     Creates an instance of RetryManager using WebTransientErrorDetectionStrategy.
        /// </summary>
        /// <param name="retryStrategyConfiguration">Specifies retry strategy to be used when a transient error is encountered.</param>
        /// <param name="logger">Used for tracing.</param>
        public RetryManager(
            IRetryStrategyConfiguration retryStrategyConfiguration,
            ILogger logger)
            :
            this(retryStrategyConfiguration, logger, WebTransientErrorDetectionStrategy.Instance, false)
        {
        }

        /// <summary>
        ///     Executes a given non-asynchronous delegate with retries for transient errors.
        /// </summary>
        /// <param name="componentName">Name of the component to which the passed delegate belongs.</param>
        /// <param name="methodName">Name of the method pointed to by the passed delegate.</param>
        /// <param name="func">The non-asynchronous delegate to be executed.</param>
        /// <returns>the execution result</returns>
        public T Execute<T>(
            string componentName,
            string methodName,
            Func<T> func)
        {
            int pass = -1;

            T RunRetriable()
            {
                try
                {
                    ++pass;
                    return func();
                }
                catch (Exception e)
                {
                    this.logger?.Warning(
                        componentName,
                        $"Retriable method [{methodName}] failed on pass {pass}: {e.GetMessageAndInnerMessages()}");
                    throw;
                }
            }

            return this.retryPolicy != null ? this.retryPolicy.ExecuteAction(this.logger == null ? func : RunRetriable) : func();
        }

        /// <summary>
        ///     Executes a given asynchronous delegate with retries for transient errors.
        /// </summary>
        /// <param name="componentName">Name of the component to which the passed delegate belongs.</param>
        /// <param name="methodName">Name of the method pointed to by the passed delegate.</param>
        /// <param name="taskFunc">The asynchronous delegate to be executed.</param>
        /// <returns>A task that is running the asynchronous delegate.</returns>
        public Task<TResult> ExecuteAsync<TResult>(
            string componentName,
            string methodName,
            Func<Task<TResult>> taskFunc)
        {
            int pass = -1;

            async Task<TResult> RunRetriableAsync()
            {
                try
                {
                    ++pass;
                    return await taskFunc();
                }
                catch (Exception e)
                {
                    this.logger?.Warning(
                        componentName,
                        $"Retriable method [{methodName}] failed on pass {pass}: {e.GetMessageAndInnerMessages()}");
                    throw;
                }
            }

            return this.retryPolicy != null ? this.retryPolicy.ExecuteAsync(this.logger == null ? taskFunc : RunRetriableAsync) : taskFunc();
        }

        /// <summary>
        ///     Executes a given asynchronous delegate with retries for transient errors.
        /// </summary>
        /// <param name="componentName">Name of the component to which the passed delegate belongs.</param>
        /// <param name="methodName">Name of the method pointed to by the passed delegate.</param>
        /// <param name="taskFunc">The asynchronous delegate to be executed.</param>
        /// <returns>A task that is running the asynchronous delegate.</returns>
        public Task ExecuteAsync(
            string componentName,
            string methodName,
            Func<Task> taskFunc)
        {
            int pass = -1;

            async Task RunRetriableAsync()
            {
                try
                {
                    ++pass;
                    await taskFunc();
                }
                catch (Exception e)
                {
                    this.logger?.Warning(
                        componentName,
                        $"Retriable method [{methodName}] failed on pass {pass}: {e.GetMessageAndInnerMessages()}");
                    throw;
                }
            }

            return this.retryPolicy != null ? this.retryPolicy.ExecuteAsync(this.logger == null ? taskFunc : RunRetriableAsync) : taskFunc();
        }

        /// <summary>
        ///     Retrieve the retry-strategy from the configuration data.
        /// </summary>
        /// <param name="configuration">The configuration data.</param>
        /// <returns>The retrieved retry-strategy.</returns>
        private static RetryStrategy GetRetryStrategy(IRetryStrategyConfiguration configuration)
        {
            if (configuration.RetryMode == RetryMode.FixedInterval)
            {
                IFixedIntervalRetryConfiguration config = configuration.FixedIntervalRetryConfiguration;
                return new FixedInterval(
                    (int)config.RetryCount,
                    TimeSpan.FromMilliseconds(config.RetryIntervalInMilliseconds));
            }

            if (configuration.RetryMode == RetryMode.ExponentialBackOff)
            {
                IExponentialBackOffRetryConfiguration config = configuration.ExponentialBackOffRetryConfiguration;
                return new ExponentialBackoff(
                    (int)config.RetryCount,
                    TimeSpan.FromMilliseconds(config.MinBackOffInMilliseconds),
                    TimeSpan.FromMilliseconds(config.MaxBackOffInMilliseconds),
                    TimeSpan.FromMilliseconds(config.DeltaBackOffInMilliseconds));
            }

            if (configuration.RetryMode == RetryMode.IncrementInterval)
            {
                IIncrementIntervalRetryConfiguration config = configuration.IncrementIntervalRetryConfiguration;
                return new Incremental(
                    (int)config.RetryCount,
                    TimeSpan.FromMilliseconds(config.InitialIntervalInMilliseconds),
                    TimeSpan.FromMilliseconds(config.IntervalIncrementInMilliseconds));
            }

            return null;
        }
    }
}
