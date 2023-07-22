namespace Microsoft.PrivacyServices.DataManagement.AzureKeyVaultCertificateInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    using Autofac;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// This program is responsible for loading certificates from Azure Key Vault, and installing them prior to our primary services
    /// executing. This program operates with some special logic in mind, to allow for it to work in situations where the primary
    /// services will not.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PDMS);

            var logger = DualLogger.Instance;
            DualLogger.AddTraceListener();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new InstrumentationModule());
            containerBuilder.RegisterModule(new FileSystemModule());

            using (var container = containerBuilder.Build())
            {
                try
                {
                    Initialize(container).GetAwaiter().GetResult();

                    var certInstaller = new CertificateInstaller(
                        container.Resolve<IEventWriterFactory>() as IEventWriterFactory,
                        container.Resolve<IPrivacyConfigurationManager>() as IPrivacyConfigurationManager);

                    await certInstaller.RunAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press [Enter] to exit");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Initialize any dependencies in parallel.
        /// </summary>
        /// <param name="container">The dependency container.</param>  
        /// <returns>A task to initialize the service.</returns>
        public static async Task Initialize(IContainer container)
        {
            var initializationTasks = container.Resolve<IEnumerable<IInitializer>>().Select(i => i.InitializeAsync()).ToArray();
            await Task.WhenAll(initializationTasks).ConfigureAwait(false);
        }
    }
}