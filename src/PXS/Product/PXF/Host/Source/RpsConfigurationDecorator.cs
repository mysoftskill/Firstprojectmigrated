// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.ServiceProcess;

    /// <summary>
    ///     RpsConfiguration Decorator
    /// </summary>
    public class RpsConfigurationDecorator : HostDecorator
    {
        private readonly string sourceConfigPath;

        private readonly Assembly assembly;

        private readonly bool loadConfigFromEmbeddedResource;

        private const string PassportRpsService = "Passport RPS Service";

        /// <summary>
        ///     Initializes a new instance of the <see cref="RpsConfigurationDecorator" /> class.
        /// </summary>
        /// <param name="sourceConfigPath">The source configuration path.</param>
        public RpsConfigurationDecorator(string sourceConfigPath)
        {
            this.sourceConfigPath = sourceConfigPath;
            this.loadConfigFromEmbeddedResource = false;
        }

        public RpsConfigurationDecorator(string resourceName, Assembly assembly)
        {
            this.sourceConfigPath = resourceName;
            this.assembly = assembly;
            this.loadConfigFromEmbeddedResource = true;
        }

        /// <summary>
        ///     Executes this instance.
        /// </summary>
        public override ConsoleSpecialKey? Execute()
        {
            // Stops RPS service
            // Copies the rpsserver.xml (defined in configuration) to the rps config directory.
            // Starts RPS service

            Trace.TraceInformation($"Executing method: {nameof(this.Execute)}");

            using (var rpsServiceController = new ServiceController(PassportRpsService))
            {
                StopService(rpsServiceController);

                Trace.TraceInformation("Begin copying RPS configuration.");

                string sourcePath =
                    Path.Combine(
                        Environment.CurrentDirectory,
                        this.sourceConfigPath);
                Trace.TraceInformation("RPS Config Path: {0}", sourcePath);

                string destinationPath =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "Microsoft Passport RPS\\config\\rpsserver.xml");
                Trace.TraceInformation("Copying configuration to destination: {0}", destinationPath);

                if (this.loadConfigFromEmbeddedResource)
                {
                    using (Stream stream = this.assembly.GetManifestResourceStream(this.sourceConfigPath))
                    using (StreamReader reader = new StreamReader(stream ?? throw new InvalidOperationException($"Stream was null for resource name: {this.sourceConfigPath}")))
                    {
                        File.WriteAllText(destinationPath, reader.ReadToEnd());
                    }
                }
                else
                {
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                }

                Trace.TraceInformation("Finished copying RPS configuration.");

                StartService(rpsServiceController);
            }

            return base.Execute();
        }

        private static void StartService(ServiceController serviceController)
        {
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                Trace.TraceInformation("Service is already started: '{0}'", PassportRpsService);
                return;
            }

            Trace.TraceInformation("Starting '{0}'", PassportRpsService);

            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));

            Trace.TraceInformation("Start successful for: '{0}'", PassportRpsService);
        }

        private static void StopService(ServiceController serviceController, int maxAttempts = 3)
        {
            Trace.TraceInformation($"Attempting to stop '{PassportRpsService}'");
            int attemptCount = 1;

            do
            {
                serviceController.Refresh();

                if (serviceController.Status == ServiceControllerStatus.Stopped)
                {
                    Trace.TraceInformation($"{PassportRpsService} is already stopped.");
                    return;
                }

                Trace.TraceInformation($"Current status is: {serviceController.Status}");

                if (serviceController.CanStop)
                {
                    Trace.TraceInformation($"{PassportRpsService} can be stopped. Attempting to stop.");

                    serviceController.Stop();

                    // wait for the service to stop
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));

                    // if it did not stop, an exception would have been thrown, so we know this succeeded.
                    Trace.TraceInformation("Stop successful for: '{0}'", PassportRpsService);
                    return;
                }

                Trace.TraceError($"Error. service: '{PassportRpsService}' cannot be stopped. Service status: {serviceController.Status}. Attempt #: {attemptCount}");
                attemptCount++;
            } while (attemptCount <= maxAttempts);

            Trace.TraceError("Could not stop the service: '{0}' after retrying: {1} times.", PassportRpsService, maxAttempts);
            throw new InvalidOperationException("Cannot stop the service: " + PassportRpsService);
        }
    }
}
