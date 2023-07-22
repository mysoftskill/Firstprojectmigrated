namespace Microsoft.PrivacyServices.DataManagement.Worker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Autofac;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Worker.Scheduler;

    /// <summary>
    /// Worker.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Program : IDisposable
    {
        /// <summary>
        /// The cancellation source. Used in unit tests and feature tests to stop execution.
        /// </summary>
        public readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Entry point for all workers.
        /// </summary>
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            
            using (var program = new Program())
            {
                GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PDMS);
                DualLogger.AddTraceListener();

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    program.CancellationTokenSource.Cancel();
                };

                var builder = AutofacConfig.RegisterComponents();

                var container = builder.Build();

                using (var scope = container.BeginLifetimeScope("PdmsWorkers"))
                {
                    var eventWriterFactory = scope.Resolve<IEventWriterFactory>() as IEventWriterFactory;
                    eventWriterFactory.Trace(nameof(Program), "Beginning PdmsWorker initializations.");

                    InitializeDependenciesAsync(scope).GetAwaiter().GetResult();

                    program.Run(scope).GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// Runs the initializers.
        /// </summary>
        /// <param name="scope">The scope in which to run the intializers.</param>
        /// <returns>The container reference.</returns>
        public static async Task InitializeDependenciesAsync(ILifetimeScope scope)
        {
            var initializationTasks = scope.Resolve<IEnumerable<IInitializer>>().Select(i => i.InitializeAsync()).ToArray();
            await Task.WhenAll(initializationTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Schedule the workers.
        /// </summary>
        /// <param name="scope">The scope in which to run the workers.</param>
        /// <returns>A task that runs all registered workers.</returns>
        public async Task Run(ILifetimeScope scope)
        {
            try
            {
                var schedulerTasks = scope.Resolve<IEnumerable<WorkScheduler>>().ToArray();

                await Task.WhenAll(schedulerTasks.Select(this.Start)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(Program), ex.ToString());
            }
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) below.
            this.Dispose(true);
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        /// <param name="disposing">Whether or not to dispose the objects.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    this.CancellationTokenSource.Dispose();
                }

                this.disposedValue = true;
            }
        }

        /// <summary>
        /// This code added to log unhandled exceptions to the current directory.
        /// </summary>
        /// <param name="sender">Where the exception came from.</param>
        /// <param name="e">Unhandled exception event arguments.</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log unhandled exceptions 
            string filePath = string.Format(@"{0}\PdmsWorkerExceptions_{1:yyyy}_{1:MM}_{1:dd}.txt", @"..\log", DateTime.Now);

            Exception ex = e.ExceptionObject as Exception;

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now);
                writer.WriteLine();

                while (ex != null)
                {
                    writer.WriteLine(ex.GetType().FullName);
                    writer.WriteLine("Message : " + ex.Message);
                    writer.WriteLine("StackTrace : " + ex.StackTrace);

                    ex = ex.InnerException;
                }
            }
        }

        /// <summary>
        /// Schedule a worker.
        /// </summary>
        /// <param name="scheduler">The scheduler to start.</param>  
        /// <returns>A task to schedule a service.</returns>
        private async Task Start(WorkScheduler scheduler)
        {
            try
            {
                await scheduler.RunAsync(this.CancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(Program), ex.ToString());
            }
        }
    }
}
