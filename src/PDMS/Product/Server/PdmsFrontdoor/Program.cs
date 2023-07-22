namespace Microsoft.PrivacyServices.DataManagement.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using global::Autofac;
    using global::Owin;
    using Microsoft.Owin.Host.HttpListener;
    using Microsoft.Owin.Hosting;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;

    using IContainer = global::Autofac.IContainer;

    /// <summary>
    /// The entry point into the frontdoor application.
    /// </summary>
    /// <remarks>
    /// This is excluded from code coverage because it is mocked in all tests.
    /// </remarks>
    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors")]
    [ExcludeFromCodeCoverage]
    public class Program
    {
        /// <summary>
        /// The set of API registrations for this service.
        /// </summary>
        public static readonly ApiRegistration[] ControllerRegistrations = new ApiRegistration[]
        {
                Registration.Initialize
        };

        /// <summary>
        /// The entry point into the host application.
        /// </summary>
        /// <param name="args">Any provided command line arguments.</param>
        public static void Main(string[] args)
        {
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PDMS);

            var logger = DualLogger.Instance;
            DualLogger.AddTraceListener();

            if (args.Length > 0 && args[0].Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                logger.Information(nameof(Program), "Waiting for debugger.");
                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }
            }

            var stopEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                stopEvent.Set();
            };

            var container = AutofacConfig.RegisterComponents().Build();

            var startOptions = new StartOptions();
            startOptions.Urls.Add($"https://+:443");

            var eventWriterFactory = container.Resolve<IEventWriterFactory>() as IEventWriterFactory;
            eventWriterFactory.Trace(nameof(Program), "Beginning initializations.");

            Initialize(container).GetAwaiter().GetResult();

            eventWriterFactory.Trace(nameof(Program), "Initializations complete.");

            // Start OWIN host and wait for console to close.
            using (WebApp.Start(startOptions, builder => BuildOwinServer(builder, container, eventWriterFactory)))
            {
                eventWriterFactory.Trace(nameof(Program), "Service started.");
                stopEvent.WaitOne();
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


        private static void BuildOwinServer(IAppBuilder builder, IContainer container, IEventWriterFactory eventWriterFactory)
        {
            var listener = builder.Properties[typeof(OwinHttpListener).FullName] as OwinHttpListener;

            if (listener != default)
            {
                eventWriterFactory.Trace(nameof(Program), "Reduce header wait to 5 seconds");
                listener.Listener.TimeoutManager.HeaderWait = TimeSpan.FromSeconds(5);
            }
            OwinStartup.Register(builder, container, ControllerRegistrations);
        }
    }
}
