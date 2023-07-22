// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Metrics
{
    using System;

    using Microsoft.Cloud.InstrumentationFramework;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     The IFX Metric initialization class that sets the default monitoring account and dimensions, and is used to create
    ///     metrics
    /// </summary>
    public class IfxMetricFactory : ICounterFactory
    {
        /// <summary>environment settings to use for metrics</summary>
        private readonly IIfxEnvironment envSettings;

        /// <summary>Where to emit trace logs</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the IfxMetricFactory class
        /// </summary>
        /// <param name="envSettings">environment</param>
        /// <param name="logger">Geneva logger</param>
        /// <param name="metricInitializer">
        ///     the entire point of this parameter is to force Unity to create a new instance of IfxMetricInitializer in a 
        ///      non-static context. It is intentionally not used
        /// </param>
        public IfxMetricFactory(
            IIfxEnvironment envSettings,
            ILogger logger,
            IIfxMetricInitializer metricInitializer)
        {
            this.envSettings = envSettings;
            this.logger = logger;
        }

        /// <summary>
        ///     Create a new one-dimensional metric
        /// </summary>
        /// <param name="categoryName">name of the group that will contain the metric</param>
        /// <param name="counterName">name of the metric</param>
        /// <param name="counterType">name of the dimension</param>
        /// <returns>new dimension</returns>
        public ICounter GetCounter(
            string categoryName,
            string counterName,
            CounterType counterType)
        {
            MeasureMetric1D metric = this.CreateMetric(
                counterName,
                context => MeasureMetric1D.Create(
                    this.envSettings.MonitoringAccount,
                    categoryName,
                    counterName,
                    "Instance",
                    ref context,
                    true));

            return new Metric1D(metric, counterName, this.logger);
        }

        /// <summary>Logs a message if the metric wasn't created</summary>
        /// <typeparam name="TMetric">type of the metric interface</typeparam>
        /// <param name="metricName">name of the method</param>
        /// <param name="create">Function that creates a new metric</param>
        /// <returns>metric created</returns>
        private TMetric CreateMetric<TMetric>(
            string metricName,
            Func<ErrorContext, TMetric> create)
        {
            ErrorContext errorContext = new ErrorContext();

            TMetric metric = create(errorContext);

            if (metric == null)
            {
                this.logger.Error("Unable to create metric {0}. Error {1}", metricName, errorContext.ErrorMessage);
            }

            return metric;
        }

        /// <summary>
        ///     Wrapper for the 1-dimensional metric, which allows using this metric without referencing the IFX assemblies
        /// </summary>
        private class Metric1D : ICounter
        {
            private const string AllInstanceName = "All";

            private readonly MeasureMetric1D metric;
            private readonly ILogger logger;
            private readonly string name;

            /// <summary>
            ///     Initializes a new instance of the Metric1D class
            /// </summary>
            /// <param name="metric">metric to wrap</param>
            /// <param name="metricName">name of the metric</param>
            /// <param name="logger">logger used to log errors</param>
            public Metric1D(
                MeasureMetric1D metric,
                string metricName,
                ILogger logger)
            {
                this.metric = metric;
                this.logger = logger;
                this.name = metricName;
            }

            /// <summary>
            ///      Sets the value
            /// </summary>
            /// <param name="value">value</param>
            public void SetValue(ulong value)
            {
                this.LogValue(Metric1D.TruncateToLong(value), null);
            }

            /// <summary>
            ///      Sets the value
            /// </summary>
            /// <param name="value">value</param>
            /// <param name="instanceName">instance name</param>
            public void SetValue(
                ulong value,
                string instanceName)
            {
                this.LogValue(Metric1D.TruncateToLong(value), instanceName);
            }

            /// <summary>
            ///      Decrement the default instance counter
            /// </summary>
            public void Decrement()
            {
                this.LogValue(-1, null);
            }

            /// <summary>
            ///      Decrement the specified instance counter
            /// </summary>
            /// <param name="instanceName">instance counter to affect</param>
            public void Decrement(string instanceName)
            { 
                this.LogValue(-1, instanceName);
            }

            /// <summary>
            ///      Decrement the default instance counter by the specific amount
            /// </summary>
            /// <param name="value">amount to Decrement by</param>
            public void DecrementBy(ulong value)
            {
                this.LogValue(-1 * Metric1D.TruncateToLong(value), null);
            }

            /// <summary>
            ///     Decrement the specified instance counter by the specific amount
            /// </summary>
            /// <param name="value">amount to Decrement by</param>
            /// <param name="instanceName">instance counter to affect</param>
            public void DecrementBy(ulong value, string instanceName)
            {
                this.LogValue(-1 * Metric1D.TruncateToLong(value), instanceName);
            }

            /// <summary>
            ///      Increment the default instance counter
            /// </summary>
            public void Increment()
            {
                this.LogValue(1, null);
            }

            /// <summary>
            ///      Increment the specified instance counter
            /// </summary>
            /// <param name="instanceName">instance counter to affect</param>
            public void Increment(string instanceName)
            {
                this.LogValue(1, instanceName);
            }

            /// <summary>
            ///      Increment the default instance counter by the specific amount
            /// </summary>
            /// <param name="value">amount to increment by</param>
            public void IncrementBy(ulong value)
            {
                this.LogValue(Metric1D.TruncateToLong(value), null);
            }

            /// <summary>
            ///     Increment the specified instance counter by the specific amount
            /// </summary>
            /// <param name="value">amount to increment by</param>
            /// <param name="instanceName">instance counter to affect</param>
            public void IncrementBy(
                ulong value, 
                string instanceName)
            {
                this.LogValue(Metric1D.TruncateToLong(value), instanceName);
            }

            /// <summary>
            ///      Get the value of the default instance counter
            /// </summary>
            /// <returns>value of the counter</returns>
            public ulong GetValue() => throw new NotImplementedException();

            /// <summary>
            ///      Get the value of the specified instance counter
            /// </summary>
            /// <param name="instanceName">instance counter to affect</param>
            /// <returns>value of the counter</returns>
            public ulong GetValue(string instanceName) => throw new NotImplementedException();

            /// <summary>
            ///      Truncates a ulong to long.MaxValue
            /// </summary>
            /// <param name="v">v</param>
            /// <returns>resulting value</returns>
            private static long TruncateToLong(ulong v) => (long)(v & long.MaxValue);

            /// <summary>Log a message if there was an error while emitting a metric</summary>
            /// <param name="metricName">name of the metric</param>
            /// <param name="errCtx">context that contains the message</param>
            private void LogMetricEmitFailure(
                string metricName,
                ErrorContext errCtx)
            {
                if (errCtx.ErrorCode != 0)
                {
                    this.logger.Error(this.GetType().Name, "Error emitting metirc {0}: {1}", metricName, errCtx.ErrorMessage);
                }
            }

            /// <summary>
            ///      Logs a value to the counter
            /// </summary>
            /// <param name="value">value to log</param>
            /// <param name="instanceName">counter instance name</param>
            private void LogValue(
                long value,
                string instanceName)
            {
                instanceName = instanceName ?? Metric1D.AllInstanceName;

                if (this.metric != null)
                {
                    ErrorContext errorContext = new ErrorContext();
                    this.metric.LogValue(DateTime.UtcNow, value, instanceName, ref errorContext);
                    this.LogMetricEmitFailure(this.name, errorContext);
                }
            }
        }
    }
}