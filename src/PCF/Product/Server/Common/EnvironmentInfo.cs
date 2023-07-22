namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;

    /// <summary>
    /// Reports information about the environment in which the service is running.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class EnvironmentInfo
    {
        private static bool initialized;
        private static readonly object SyncRoot;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static EnvironmentInfo()
        {
            SyncRoot = new object();
            HostingEnvironment = new OneBoxHostingEnvironment();

            // This is ugly and hacky and I hate it, but we need a way to check to see if we are running in a UT context.
            // so we can avoid referencing the APSDK, which seems to only support x64 with special app.config.
            const string TestAssemblyName = "xunit.core";
            IsUnitTest = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.FullName.StartsWith(TestAssemblyName, StringComparison.OrdinalIgnoreCase));

            if (IsUnitTest)
            {
                Initialize("UnitTestService");
            }
        }

        /// <summary>
        /// Initializes the EnvironmentInfo class.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        public static void Initialize(string serviceName)
        {
            lock (SyncRoot)
            {
                if (initialized)
                {
                    throw new InvalidOperationException("EnvironmentInfo has already been initialized.");
                }

                initialized = true;

                ServiceName = serviceName;
                AssemblyVersion = FileVersionInfo.GetVersionInfo(typeof(EnvironmentInfo).Assembly.Location).FileVersion;

                if (IsUnitTest)
                {
                    HostingEnvironment = new UnitTestHostingEnvironment();
                }
                else if (AzureServiceFabricHostingEnvironment.CanInitialize)
                {
                    HostingEnvironment = new AzureServiceFabricHostingEnvironment(serviceName);
                }
                else
                {
                    HostingEnvironment = new OneBoxHostingEnvironment();
                }
            }
        }

        /// <summary>
        /// Gets the current hosting environment.
        /// </summary>
        public static IHostingEnvironment HostingEnvironment { get; private set; }

        /// <summary>
        /// IsOneBoxEnvironment
        /// </summary>
        public static bool IsDevBoxEnvironment => HostingEnvironment.IsDevMachine;

        /// <summary>
        /// Gets the Autopilot name of the service that we're running as.
        /// </summary>
        public static string ServiceName { get; private set; }

        /// <summary>
        /// Gets the build version.
        /// </summary>
        public static string AssemblyVersion { get; private set; }

        /// <summary>
        /// Indicates that this is a unit-test context. 
        /// </summary>
        public static bool IsUnitTest { get; }

        /// <summary>
        /// Indicates if we are running in an Azure-managed environment.
        /// </summary>
        public static bool IsHostedEnvironment => !HostingEnvironment.IsDevMachine;

        /// <summary>
        /// Gets the name of the machine.
        /// </summary>
        public static string NodeName => HostingEnvironment.NodeName;

        /// <summary>
        /// Gets the name of the autopilot environment.
        /// </summary>
        public static string EnvironmentName => HostingEnvironment.EnvironmentName;
    }
}
