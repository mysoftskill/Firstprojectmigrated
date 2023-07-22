//--------------------------------------------------------------------------------
// <copyright file="RequestExecutionHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Logging
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;

    /// <summary>
    /// Helper methods for tracking execution of a delegate. Given a delegate, latency and result of the delegate are captured.
    /// Also, standard perfcounters are updated.
    /// </summary>
    public static class RequestExecutionHelper
    {
        /// <summary>
        /// Executes an async delegate thereby capturing it's latency and exception (if any).
        /// Subsequently, standard API perf counters are updated.
        /// </summary>
        /// <param name="counterFactory">Factory used for creating counters.</param>
        /// <param name="componentName">Name of the module to which the delegate belongs.</param>
        /// <param name="methodName">Name of the delegate to be executed.</param>
        /// <param name="action">The async delegate to be executed.</param>
        /// <returns>A task containing result of execution of the delegate.</returns>
        public static async Task<TimedOperationExecutionResult> ExecuteTimedActionAsync(
            ICounterFactory counterFactory, string componentName, string methodName, Func<Task> action)
        {
            var result = new TimedOperationExecutionResult();
            var timer = Stopwatch.StartNew();

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                ex.PrepareForRemoting();
                result.Exception = ex;
            }
            finally
            {
                timer.Stop();
                result.LatencyInMilliseconds = (ulong)timer.ElapsedMilliseconds;
            }

            PerfCounterHelper.UpdatePerfCounters(counterFactory, componentName, methodName, result);

            return result;
        }

        /// <summary>
        /// Executes an async delegate thereby capturing it's latency and result (including exception).
        /// Subsequently, standard API perf counters are updated.
        /// </summary>
        /// <typeparam name="TResult">Return type of the delegate to be executed.</typeparam>
        /// <param name="counterFactory">Factory used for creating counters.</param>
        /// <param name="componentName">Name of the module to which the delegate belongs.</param>
        /// <param name="methodName">Name of the delegate to be executed.</param>
        /// <param name="action">The async delegate to be executed.</param>
        /// <returns>A task containing result of execution of the delegate.</returns>
        public static async Task<TimedOperationExecutionResult<TResult>> ExecuteTimedActionAsync<TResult>(
            ICounterFactory counterFactory, string componentName, string methodName, Func<Task<TResult>> action)
        {
            return await ExecuteTimedActionAsync<TimedOperationExecutionResult<TResult>, TResult>(
                counterFactory,
                componentName, 
                methodName, 
                action);
        }

        /// <summary>
        /// Executes an async HTTP delegate thereby capturing it's latency and result (including exception).
        /// Subsequently, standard API as well as HTTP specific perf counters are updated.
        /// </summary>
        /// <param name="counterFactory">Factory used for creating counters.</param>
        /// <param name="componentName">Name of the module to which the delegate belongs.</param>
        /// <param name="methodName">Name of the delegate to be executed.</param>
        /// <param name="action">The async HTTP delegate to be executed.</param>
        /// <returns>A task containing result of execution of the delegate.</returns>
        public static async Task<TimedHttpOperationExecutionResult> ExecuteTimedHttpActionAsync(
            ICounterFactory counterFactory, string componentName, string methodName, Func<Task<HttpResponseMessage>> action)
        {
            return await ExecuteTimedActionAsync<TimedHttpOperationExecutionResult, HttpResponseMessage>(
                counterFactory,
                componentName,
                methodName,
                action);
        }

        /// <summary>
        /// Executes an async delegate thereby capturing it's latency and result (including exception).
        /// Subsequently, standard API perf counters are updated.
        /// </summary>
        /// <typeparam name="TimedOperationResult">The return-type of the delegate to be executed.</typeparam>
        /// <typeparam name="TResult">The result of the return-type of the delegate to be executed.</typeparam>
        /// <param name="counterFactory">Factory used for creating counters.</param>
        /// <param name="componentName">Name of the module to which the delegate belongs.</param>
        /// <param name="methodName">Name of the delegate to be executed.</param>
        /// <param name="action">The async delegate to be executed.</param>
        /// <returns>A task containing result of execution of the delegate.</returns>
        private static async Task<TimedOperationResult> ExecuteTimedActionAsync<TimedOperationResult, TResult>(
            ICounterFactory counterFactory,
            string componentName, 
            string methodName,
            Func<Task<TResult>> action)
            where TimedOperationResult : TimedOperationExecutionResult<TResult>, new()
        {
            var result = new TimedOperationResult();
            var timer = Stopwatch.StartNew();

            try
            {
                result.Response = await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ex.PrepareForRemoting();
                result.Exception = ex;
            }
            finally
            {
                timer.Stop();
                result.LatencyInMilliseconds = (ulong)timer.ElapsedMilliseconds;
            }

            PerfCounterHelper.UpdatePerfCounters(counterFactory, componentName, methodName, result);

            return result;
        }
    }
}
