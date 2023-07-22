namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    /// <summary>
    /// A factory that resolves type requests for specific ISessionWriter classes.
    /// </summary>
    public interface ISessionWriterFactory
    {
        /// <summary>
        /// Tries to create a session writer that can log the given type.
        /// </summary>
        /// <remarks>
        /// SessionType is provided so that you can register different events for different session types.
        /// For example, Incoming and Outgoing API calls need different events for XPERT to register properly.
        /// </remarks>
        /// <typeparam name="TData">The type for the session writer.</typeparam>
        /// <param name="sessionType">The session type.</param>
        /// <param name="writer">The discovered session writer.</param>
        /// <returns>True if a session writer could be created, otherwise false.</returns>
        bool TryCreate<TData>(SessionType sessionType, out ISessionWriter<TData> writer);
    }

    /// <summary>
    /// Extension functions for the session writer factory classes.
    /// </summary>
    public static class SessionWriterFactoryExtensions
    {
        /// <summary>
        /// Writes the completed session information if it is able to find a corresponding session writer.
        /// Otherwise, drops the event data.
        /// </summary>
        /// <typeparam name="TData">The type for the session writer.</typeparam>
        /// <param name="sessionWriterFactory">The session writer factory class.</param>
        /// <param name="sessionType">The session type. Necessary for discovering the session writer.</param>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">Additional metadata.</param>
        public static void WriteDone<TData>(this ISessionWriterFactory sessionWriterFactory, SessionType sessionType, SessionStatus status, string name, long totalMilliseconds, string cv, TData data)
        {
            ISessionWriter<TData> writer;
            if (sessionWriterFactory.TryCreate(sessionType, out writer))
            {
                writer.WriteDone(status, name, totalMilliseconds, cv, data);
            }
        }
    }
}
