namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Threading.Tasks;

    /// <summary>
    /// A factory that resolves type requests for specific IEventWriter classes.
    /// </summary>
    public interface IEventWriterFactory
    {
        /// <summary>
        /// Attempts to create an event writer for the given type.
        /// </summary>
        /// <typeparam name="TData">The type whose event writer is requested.</typeparam>
        /// <param name="writer">An event writer that can log data of the given type.</param>
        /// <returns>True if a writer was created, otherwise false.</returns>
        bool TryCreate<TData>(out IEventWriter<TData> writer);
    }

    /// <summary>
    /// Extension methods for the event writer factory classes.
    /// </summary>
    public static class EventWriterFactoryExtensions
    {
        /// <summary>
        /// Writes the given data as an event.
        /// </summary>
        /// <typeparam name="TData">The data type whose event writer will be used.</typeparam>
        /// <param name="eventWriterFactory">The event factory to extend.</param>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The data to log.</param>
        /// <param name="eventLevel">The event level for the event.</param>
        public static void WriteEvent<TData>(this IEventWriterFactory eventWriterFactory, string componentName, TData data, EventLevel eventLevel = EventLevel.Informational)
        {
            IEventWriter<TData> writer;
            if (eventWriterFactory.TryCreate(out writer))
            {
                writer.WriteEvent(componentName, data, eventLevel);
            }
        }

        /// <summary>
        /// Logs arbitrary messages as a Verbose event.
        /// </summary>
        /// <param name="eventWriterFactory">The event factory to extend.</param>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The message to log.</param>
        public static void Trace(this IEventWriterFactory eventWriterFactory, string componentName, string data)
        {
            IEventWriter<string> writer;
            if (eventWriterFactory.TryCreate(out writer))
            {
                writer.WriteEvent(componentName, data, System.Diagnostics.Tracing.EventLevel.Informational);
            }
        }

        /// <summary>
        /// Logs a suppressed exception as a warning.
        /// </summary>
        /// <param name="eventWriterFactory">The event factory to extend.</param>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The suppressed exception to log.</param>
        public static void SuppressedException(this IEventWriterFactory eventWriterFactory, string componentName, SuppressedException data)
        {
            IEventWriter<SuppressedException> writer;
            if (eventWriterFactory.TryCreate(out writer))
            {
                writer.WriteEvent(componentName, data, EventLevel.Warning);
            }
        }

        /// <summary>
        /// Executes code and suppresses any exceptions that would normally be thrown.
        /// Any suppressed exception is logged as a warning.
        /// </summary>
        /// <param name="eventWriterFactory">The event factory to extend.</param>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="actionName">A name to describe the action that is suppressing exceptions.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>The original action decorated with exception handling.</returns>
        public static async Task SuppressExceptionAsync(this IEventWriterFactory eventWriterFactory, string componentName, string actionName, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SuppressedException(eventWriterFactory, componentName, new SuppressedException(actionName, ex));
            }
        }
    }
}
