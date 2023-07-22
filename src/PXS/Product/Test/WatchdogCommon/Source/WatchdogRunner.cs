// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.WatchdogCommon
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Contracts.Adapter.DeviceManager;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Helper class for abstracting the logic for running watchdogs accross machines.
    /// </summary>
    public class WatchdogRunner
    {
        private const string ComponentName = nameof(WatchdogRunner);

        private readonly IDeviceManager deviceManager;
        private readonly ILogger logger;
        private readonly string environmentName;
        private readonly WatchdogResult watchdogHeartbeatResult;

        public WatchdogRunner(
            IDeviceManager deviceManager, ILogger logger, string environmentName, WatchdogResult watchdogHeartbeatResult)
        {
            this.deviceManager = deviceManager;
            this.logger = logger;
            this.environmentName = environmentName;
            this.watchdogHeartbeatResult = watchdogHeartbeatResult;
        }

        /// <summary>
        /// Runs a watchdog check asynchronously on all machines of a given machine function in a given environment.
        /// </summary>
        /// <param name="machineFunction">The machine function.</param>
        /// <param name="check">The check.</param>
        /// <returns>A that will finish when all checks finish</returns>
        public async Task RunAsync(string machineFunction, MachineCheck check)
        {
            const string MethodName = "RunAsync";
            this.logger.MethodEnter(ComponentName, MethodName);

            GetMachineInfoRequest getMachineInfoRequest = new GetMachineInfoRequest
            {
                EnvironmentName = this.environmentName,
                MachineFunction = machineFunction
            };
            GetMachineInfoResponse getMachineInfoResponse = await this.deviceManager.GetMachineInfo(getMachineInfoRequest).ConfigureAwait(false);

            // GetMachineInfo call was successful.
            if (null != getMachineInfoResponse && null == getMachineInfoResponse.ErrorInfo)
            {
                if (getMachineInfoResponse.Machines.Any())
                {
                    ConcurrentBag<WatchdogResult> watchdogResults = new ConcurrentBag<WatchdogResult>();
                    List<Task> allTasks = new List<Task>();

                    foreach (MachineInfo apMachineInfo in getMachineInfoResponse.Machines)
                    {
                        Task healthCheckTask = check.RunAsync(apMachineInfo.MachineName, watchdogResults);
                        allTasks.Add(healthCheckTask);
                    }

                    this.logger.Information(ComponentName, "Waiting for all the checks to run or timeout");

                    // Wait for all checks to run or timeout.
                    await Task.WhenAll(allTasks).ConfigureAwait(false);

                    this.logger.Information(ComponentName, "Finished running all the checks.");

                    // Upload the results to the Device Manager.
                    await this.UploadWatchdogResults(watchdogResults).ConfigureAwait(false);
                }
                else
                {
                    this.logger.Information(ComponentName, "GetMachineInfo call was success, but no machines returned from the Device Manager. Hence not running any checks. GetMachineInfoResponse : {0}", getMachineInfoResponse);
                }
            }
            else
            {
                this.logger.Error(ComponentName, "Error getting machine info from the device manager. GetMachineInfoResponse : {0}", getMachineInfoResponse);
            }

            this.logger.MethodExit(ComponentName, MethodName);
        }

        /// <summary>
        /// This method uploads the watchdog results to the Device Manager.
        /// </summary>
        /// <param name="watchdogResults">The results to upload.</param>
        /// <returns>Task object</returns>
        private async Task UploadWatchdogResults(ConcurrentBag<WatchdogResult> watchdogResults) 
        {
            const string MethodName = "UploadWatchdogResults";
            this.logger.MethodEnter(ComponentName, MethodName);

            // Add watchdog heartbeat result. This is used by the watchdog watcher to ensure that the watchdog
            // is still running
            if (this.watchdogHeartbeatResult != null)
            {
                watchdogResults.Add(this.watchdogHeartbeatResult);   
            }

            UpdateMachinePropertyRequest updateMachinePropertyRequest = new UpdateMachinePropertyRequest
            {
                EnvironmentName = this.environmentName,
                WatchdogResults = watchdogResults
            };

            UpdateMachinePropertyResponse updateMachinePropertyResponse = await this.deviceManager.UpdateMachineProperty(updateMachinePropertyRequest).ConfigureAwait(false);

            if (null == updateMachinePropertyResponse || null != updateMachinePropertyResponse.ErrorInfo)
            {
                this.logger.Error(ComponentName, "Error uploading watchdog results to the device manager. UpdateMachinePropertyResponse : {0}", updateMachinePropertyResponse);
            }

            this.logger.MethodExit(ComponentName, MethodName);
        }
    }
}
