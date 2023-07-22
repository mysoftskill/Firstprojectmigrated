// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Defines a method that supports startup/execution of a service host.
    /// </summary>
    public interface IHost
    {
        ConsoleSpecialKey? Execute();
    }

    /// <summary>
    ///     A base type for host handlers that delegate the processing of host startup/execution to another handler, called the inner handler.
    /// </summary>
    public abstract class HostDecorator : IHost
    {
        public IHost InnerHandler { get; set; }

        public virtual ConsoleSpecialKey? Execute()
        {
            return this.InnerHandler.Execute();
        }
    }

    /// <summary>
    ///     A service host which waits indefinitely for either a Ctrl+C or Ctrl+Break command.
    /// </summary>
    public class ConsoleHost : IHost, IDisposable
    {
        private static bool shutdown = false;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private bool disposed;

        /// <summary>
        ///     The currently running ConsoleHost.
        /// </summary>
        public static ConsoleHost Instance { get; private set; }

        /// <summary>
        ///     Cancellation Token
        /// </summary>
        public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

        /// <summary>
        ///     Initializes a new instance of the ConsoleHost class
        /// </summary>
        public ConsoleHost()
        {
            Instance = this;
        }

        /// <summary>
        ///     Implement <see cref="IDisposable" />; dispose resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("Host executed");

            // Establish an event handler to process key press events.
            Console.CancelKeyPress += new ConsoleCancelEventHandler(shutdownHandler);

            Console.WriteLine("Press Ctrl-C to exit.");

            while (!shutdown)
            {
                Task.Delay(3000).GetAwaiter().GetResult();
            }

            ConsoleSpecialKey? key = ConsoleHost.shutdown ? ConsoleSpecialKey.ControlC : (ConsoleSpecialKey?)null;
            this.PrepareShutdown(key);

            return ConsoleSpecialKey.ControlC;
        }

        /// <summary>
        ///     Dispose underlying resources.
        /// </summary>
        /// <param name="disposing">true if called from <see cref="System.IDisposable" />; otherwise false.</param>
        private void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
                this.cancellationTokenSource?.Dispose();
            this.disposed = true;
        }

        /// <summary>
        ///     Prepare to shutdown the application
        /// </summary>
        private void PrepareShutdown(ConsoleSpecialKey? consoleSpecialKey)
        {
            Trace.TraceInformation($"Signal received: {consoleSpecialKey}. Preparing application to shutdown.");
            this.cancellationTokenSource.Cancel();
            Trace.TraceInformation($"Waiting 30 secs for active connections to be gracefully closed.");
            Trace.Flush();
            Task.Delay(30000).GetAwaiter().GetResult();
        }

        protected static void shutdownHandler(object sender, ConsoleCancelEventArgs args)
        {
            // Set the Cancel property to true to prevent the process from terminating.
            Console.WriteLine("Setting the Cancel property to true...");
            args.Cancel = true;

            ConsoleHost.shutdown = true;
        }
    }

    /// <summary>
    ///     A non blocking host that does not wait
    /// </summary>
    public class NonBlockinghost : IHost
    {
        public ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("Host executed");
            return null;
        }
    }

    /// <summary>
    ///     Represents the factory for creating new ConsoleHost instances.
    /// </summary>
    public static class HostFactory
    {
        public static IHost CreateNonBlockingPipeline(params HostDecorator[] decorators)
        {
            return CreatePipeline(new NonBlockinghost(), decorators);
        }

        /// <summary>
        ///     Creates a new instance of ConsoleHost which should be pipelined. The decorators are pipelined so that
        ///     startup/execution starts from left-to-right.
        /// </summary>
        /// <param name="decorators">The list of decorators that delegate the startup/execution of the service host to another handler.</param>
        /// <returns>A new instance of ConsoleHost which should be pipelined.</returns>
        public static IHost CreatePipeline(params HostDecorator[] decorators)
        {
            return CreatePipeline(new ConsoleHost(), decorators);
        }

        private static IHost CreatePipeline(IHost innerHost, params HostDecorator[] decorators)
        {
            // Reverse the provided decorators since the pipeline is built inside-out
            IHost pipeline = innerHost;
            foreach (HostDecorator current in decorators.Reverse())
            {
                current.InnerHandler = pipeline;
                pipeline = current;
            }

            return pipeline;
        }
    }
}
