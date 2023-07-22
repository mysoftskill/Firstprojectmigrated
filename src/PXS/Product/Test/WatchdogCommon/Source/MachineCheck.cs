// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.WatchdogCommon
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Contracts.Adapter.DeviceManager;
    using Microsoft.Oss.Membership.CommonCore.Extensions;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     The definition of a check against a machine that all other checks extend
    /// </summary>
    public abstract class MachineCheck
    {
        private const string ComponentName = nameof(MachineCheck);

        private readonly ILogger logger;

        private readonly string machineCheckPropertyName;

        private readonly TimeSpan timeout;

        /// <summary>
        ///     Runs this machine health check using the Get Results Async check.
        /// </summary>
        /// <param name="targetMachineName">The <see cref="MachineInfo" /> to run the health check against</param>
        /// <param name="watchdogResults">The list results that this check will add to.</param>
        /// <returns><see cref="Task" /> which runs this check</returns>
        public async Task RunAsync(string targetMachineName, ConcurrentBag<WatchdogResult> watchdogResults)
        {
            targetMachineName.ArgumentThrowIfNull("targetMachineName");
            watchdogResults.ArgumentThrowIfNull("watchdogResults");

            // Execute the machine check with the timeout
            Task resultsTask = this.GetResultsAsync(targetMachineName, watchdogResults);
            Task completedTask = await Task.WhenAny(resultsTask, Task.Delay(this.timeout)).ConfigureAwait(false);

            // Log the MachineCheck property based on whether the MachineCheck completed within the timeout or threw an
            // exception
            MachinePropertyLevel machineCheckPropertyResult;
            string machineCheckPropertyMessage;
            if (completedTask == resultsTask)
            {
                // The MachineCheck completed within the timeout
                if (resultsTask.Exception == null)
                {
                    // The MachineCheck completed without an exception, so log OK
                    machineCheckPropertyResult = MachinePropertyLevel.Ok;
                    machineCheckPropertyMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "MachineCheck completed within {0}",
                        this.timeout);
                    this.logger.Information(ComponentName, machineCheckPropertyMessage);
                }
                else
                {
                    // The MachineCheck completed with an exception, so log a Warning. If the implemented MachineCheck
                    // is properly coded, then this should never happen. Do not log an error since that would cause
                    // repair actions
                    machineCheckPropertyResult = MachinePropertyLevel.Warning;
                    machineCheckPropertyMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "MachineCheck completed with an exception: {0}",
                        resultsTask.Exception);
                    this.logger.Error(ComponentName, machineCheckPropertyMessage);
                }
            }
            else
            {
                // The MachineCheck did not complete within the timeout, so log a Warning. Do not log an error since that
                // would cause repair actions
                machineCheckPropertyResult = MachinePropertyLevel.Warning;
                machineCheckPropertyMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "MachineCheck did not complete within {0}",
                    this.timeout);
                this.logger.Error(ComponentName, machineCheckPropertyMessage);
            }

            watchdogResults.Add(
                new WatchdogResult
                {
                    MachineName = targetMachineName,
                    WatchdogProperty = this.machineCheckPropertyName,
                    MachinePropertyLevel = machineCheckPropertyResult,
                    Message = machineCheckPropertyMessage
                });
        }

        /// <summary>
        ///     Creates a new MachineCheck.
        ///     When the MachineCheck is finished, it will create an additional WatchdogResult for the overall MachineCheck
        ///     under machineCheckPropertyName. If the MachineCheck timed out or finished with an exception, then this
        ///     result will be Warning; otherwise Ok.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="timeout">The timeout</param>
        /// <param name="machineCheckPropertyName">The machine check property name</param>
        protected MachineCheck(ILogger logger, TimeSpan timeout, string machineCheckPropertyName)
        {
            logger.ArgumentThrowIfNull("logger");
            if (timeout == default(TimeSpan))
            {
                throw new ArgumentException("value is default", "timeout");
            }

            machineCheckPropertyName.ArgumentThrowIfNull("machineCheckPropertyName");

            this.logger = logger;
            this.timeout = timeout;
            this.machineCheckPropertyName = machineCheckPropertyName;
        }

        /// <summary>
        ///     Method to be overridden with the actual check to run.
        /// </summary>
        /// <param name="targetMachineName"><see cref="MachineInfo" /> to run this check against</param>
        /// <param name="watchdogResults">The list results that this check will add to.</param>
        /// <returns>A task that returns a list of <see cref="WatchdogResult" /> resulting from the run </returns>
        protected abstract Task GetResultsAsync(string targetMachineName, ConcurrentBag<WatchdogResult> watchdogResults);

        /// <summary>
        ///     Creates individual WatchdogResults for a set of test results, as well as an aggregate WatchdogResult based
        ///     on failure percentage threshold.
        ///     The aggregate WatchdogResult is logged as an Error if (# tests failed / # tests passed OR failed) >= failure
        ///     threshold. Otherwise, it is logged as Ok. Note that tests with unknown status are NOT included in
        ///     this percentage.
        /// </summary>
        /// <param name="targetMachineName">The target machine name for which WatchdogResults will be created.</param>
        /// <param name="testResults">The test results.</param>
        /// <param name="failureThresholdInPercent">
        ///     The failure threshold percent at which the aggregate WatchdogResult
        ///     is an Error.
        /// </param>
        /// <param name="watchdogPropertyPrefix">The watchdog property prefix.</param>
        /// <returns>The WatchdogResults</returns>
        protected static List<WatchdogResult> CreateAggregateResults(
            string targetMachineName,
            TestResult[] testResults,
            int failureThresholdInPercent,
            string watchdogPropertyPrefix)
        {
            List<WatchdogResult> watchdogResults = new List<WatchdogResult>();

            // Go through the test results and count the number of passed and unknown tests
            // Create watchdog results for each of the test results. Failures for individual test results are only logged
            // as warnings
            int numPassed = 0;
            int numUnknown = 0;
            int numRunning = 0;
            foreach (TestResult testResult in testResults)
            {
                if (testResult.Status == TestStatus.Pass)
                {
                    numPassed++;
                }
                else if (testResult.Status == TestStatus.Unknown)
                {
                    numUnknown++;
                }
                else if (testResult.Status == TestStatus.Running)
                {
                    numRunning++;
                }

                watchdogResults.Add(CreateWatchdogResult(testResult, targetMachineName, watchdogPropertyPrefix, MachinePropertyLevel.Warning));
            }

            // Create an aggregate result that is logged as an Error only if a percentage threshold of the tests failed.
            //
            // Calculate the percentage failed. Exclude results that are Unknown from the percentage. We don't count
            // Unknown results as either passed or failed because if we are running against an older version of the
            // service, then unsupported APIs can throw off the percentage, incorrectly causing an Error result.
            // Also, removing the long haul tests which may take a few iterations to complete.
            int numFailed = testResults.Length - numPassed - numUnknown - numRunning;
            int numPassedOrFailed = numPassed + numFailed;
            int percentFailure;
            if (numPassedOrFailed == 0)
            {
                // Avoid divide by zero
                percentFailure = 0;
            }
            else
            {
                percentFailure = (int)Math.Round((double)numFailed / numPassedOrFailed * 100d);
            }

            // Compute aggregate result and message
            bool aggregatePass = percentFailure < failureThresholdInPercent;
            string aggregateMessage = string.Format(
                CultureInfo.InvariantCulture,
                "{0} passed, {1} unknown, of {2} tests",
                numPassed,
                numUnknown,
                testResults.Length);

            TestResult result = new TestResult
            {
                Status = aggregatePass ? TestStatus.Pass : TestStatus.Fail,
                Name = "CheckApis",
                Message = aggregateMessage
            };

            watchdogResults.Add(CreateWatchdogResult(result, targetMachineName, watchdogPropertyPrefix, MachinePropertyLevel.Error));
            return watchdogResults;
        }

        /// <summary>
        ///     Creates a watchdog result from a test result.
        /// </summary>
        /// <param name="testResult">The test result.</param>
        /// <param name="machineName">The machine name.</param>
        /// <param name="watchdogPropertyPrefix">The watchdog property prefix.</param>
        /// <param name="failurePropertyLevel">The property level to use if testResult.Status is Fail.</param>
        /// <returns>The watchdog result.</returns>
        /// <exception cref="System.ArgumentException">testResult</exception>
        protected static WatchdogResult CreateWatchdogResult(
            TestResult testResult,
            string machineName,
            string watchdogPropertyPrefix,
            MachinePropertyLevel failurePropertyLevel)
        {
            MachinePropertyLevel actualLevel;
            switch (testResult.Status)
            {
                case TestStatus.Pass:
                    actualLevel = MachinePropertyLevel.Ok;
                    break;
                case TestStatus.Fail:
                    actualLevel = failurePropertyLevel;
                    break;
                case TestStatus.Unknown:
                    actualLevel = MachinePropertyLevel.Information;
                    break;
                case TestStatus.Running:
                    actualLevel = MachinePropertyLevel.Information;
                    // When a long running test is reporting running, we don't want to blow away the last success/fail status,
                    // So report the running heartbeat on a different watchdog name.
                    watchdogPropertyPrefix += "Running_";
                    break;
                default:
                    string message = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} could not be converted to MachinePropertyLevel",
                        testResult.Status);
                    throw new ArgumentException(message, "testResult");
            }

            return new WatchdogResult
            {
                MachineName = machineName,
                WatchdogProperty = watchdogPropertyPrefix + testResult.Name,
                Message = testResult.Message,
                MachinePropertyLevel = actualLevel
            };
        }
    }
}
