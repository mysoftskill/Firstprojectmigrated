//--------------------------------------------------------------------------------
// <copyright file="PerfCounterHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.PerfCounters
{
    using System;
    using System.Net;
    using System.Net.Http;
    using Microsoft.Membership.MemberServices.Common.Logging;

    /// <summary>
    /// Miscellaneous performance-counter methods.
    /// </summary>
    public static class PerfCounterHelper
    {
        /// <summary>
        /// Updates performance-counter values based on the results of an operation.
        /// </summary>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="categoryName">The counter category name.</param>
        /// <param name="counterInstanceName">The counter name.</param>
        /// <param name="operationExecutionResult">The operation results.</param>
        public static void UpdatePerfCounters(ICounterFactory counterFactory, string categoryName, string counterInstanceName, TimedOperationExecutionResult operationExecutionResult)
        {
            UpdatePerfCounters(counterFactory, categoryName, counterInstanceName, operationExecutionResult.IsSuccess, operationExecutionResult.LatencyInMilliseconds);
        }

        /// <summary>
        /// Updates performance-counter values based on the results of an operation.
        /// </summary>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="categoryName">The counter category name.</param>
        /// <param name="counterInstanceName">The counter name.</param>
        /// <param name="isSuccess">true if operation was successful; otherwise false.</param>
        /// <param name="latencyInMilliseconds">The operation execution duration.</param>
        public static void UpdatePerfCounters(ICounterFactory counterFactory, string categoryName, string counterInstanceName, bool isSuccess, ulong latencyInMilliseconds)
        {
            ICounter requestsCounter = counterFactory.GetCounter(categoryName, CounterNames.Requests, CounterType.Number);
            ICounter requestRateCounter = counterFactory.GetCounter(categoryName, CounterNames.RequestsPerSecond, CounterType.Rate);
            ICounter latencyCounter = counterFactory.GetCounter(categoryName, CounterNames.Latency, CounterType.Number);
            ICounter latencyPercentileCounter = counterFactory.GetCounter(categoryName, CounterNames.PercentileLatency, CounterType.NumberPercentile);
            ICounter errorRateCounter = counterFactory.GetCounter(categoryName, CounterNames.ErrorsPerSecond, CounterType.Rate);

            // Requests Total.
            requestsCounter.Increment();
            requestsCounter.Increment(counterInstanceName);

            // Request Rate.
            requestRateCounter.Increment();
            requestRateCounter.Increment(counterInstanceName);

            // Latency.
            latencyCounter.SetValue(latencyInMilliseconds);
            latencyCounter.SetValue(latencyInMilliseconds, counterInstanceName);

            // Latency Percentile
            latencyPercentileCounter.SetValue(latencyInMilliseconds);
            latencyPercentileCounter.SetValue(latencyInMilliseconds, counterInstanceName);

            // Error Rate Counter.
            if (!isSuccess)
            {
                errorRateCounter.Increment();
                errorRateCounter.Increment(counterInstanceName);
            }
        }

        /// <summary>
        /// Updates performance-counter values based on the results of an operation.
        /// </summary>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="categoryName">The counter category name.</param>
        /// <param name="counterInstanceName">The counter name.</param>
        /// <param name="httpOperationExecutionResult">The operation results.</param>
        public static void UpdateHttpResponsePerfCounters(ICounterFactory counterFactory, string categoryName, string counterInstanceName, TimedHttpOperationExecutionResult httpOperationExecutionResult)
        {
            HttpResponseMessage response = httpOperationExecutionResult.Response;

            // If HTTP Operation is not successful, update additional HTTP Counters.
            if (response != null && !response.IsSuccessStatusCode)
            {
                int statusCode = (int)response.StatusCode;

                if (statusCode >= 400 && statusCode < 500)
                {
                    PerfCounterHelper.UpdateHttpFourXXCounter(counterFactory, categoryName, counterInstanceName);
                }
                else if (statusCode >= 500 && statusCode < 600)
                {
                    PerfCounterHelper.UpdateHttpFiveXXCounter(counterFactory, categoryName, counterInstanceName);
                }
            }
        }

        /// <summary>
        /// Update HTTP 4** counters.
        /// </summary>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="categoryName">The counter category name.</param>
        /// <param name="counterInstanceName">The counter name.</param>
        public static void UpdateHttpFourXXCounter(ICounterFactory counterFactory, string categoryName, string counterInstanceName)
        {
            var counter = counterFactory.GetCounter(categoryName, CounterNames.Error4XXPerSecond, CounterType.Rate);
            counter.Increment();
            counter.Increment(counterInstanceName);
        }

        /// <summary>
        /// Update HTTP 5** counters.
        /// </summary>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="categoryName">The counter category name.</param>
        /// <param name="counterInstanceName">The counter name.</param>
        public static void UpdateHttpFiveXXCounter(ICounterFactory counterFactory, string categoryName, string counterInstanceName)
        {
            var counter = counterFactory.GetCounter(categoryName, CounterNames.Error5XXPerSecond, CounterType.Rate);
            counter.Increment();
            counter.Increment(counterInstanceName);
        }

        public static void UpdateHttpConnectionCountCounter(ICounterFactory counterFactory, ServicePoint servicePoint)
        {
            string host = servicePoint.Address.Host;
            int connectionCount = servicePoint.CurrentConnections;

            var counter = counterFactory.GetCounter(CounterCategoryNames.PrivacyExperienceServiceConnections, CounterNames.ConnectionCount, CounterType.Number);
            counter.SetValue((ulong)connectionCount);
            counter.SetValue((ulong)connectionCount, host);
        }
    }
}
